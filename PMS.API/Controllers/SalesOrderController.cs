using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.Services.SalesOrder;
using PMS.Application.Services.VNpay;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesOrderController : ControllerBase
    {
        private readonly ISalesOrderService _service;

        public SalesOrderController(ISalesOrderService service)
        {
            _service = service;
        }

        private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("get-quotation-info/{quotationId}")]
        //[Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> GetQuotationInfo(int quotationId)
        {
            var result = await _service.GetQuotationInfo(quotationId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: http://localhost:5137/api/SalesOrder/send/{salesOrderId}
        /// Customer gửi đơn (Draft -> Send). Hệ thống kiểm tra tồn kho và cảnh báo
        /// tới PURCHASES_STAFF nếu thiếu/sắp hết.
        /// </summary>
        [HttpPost("send/{salesOrderId}")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> SendOrder(int salesOrderId)
        {
            var result = await _service.SendOrderAsync(salesOrderId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: http://localhost:5137/api/SalesOrder/approve/{salesOrderId}
        /// Sau khi customer send SalesOrder nếu đủ số lượng hàng sẽ được approved
        /// <param name="salesOrderId"></param>
        /// <returns></returns>
        [HttpPost("approve/{salesOrderId}")]
        [Authorize(Roles = UserRoles.SALES_STAFF)]
        public async Task<IActionResult> ApproveOrder(int salesOrderId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }
            var result = await _service.ApproveSalesOrderAsync(salesOrderId, currentUserId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: http://localhost:5137/api/SalesOrder/reject/{salesOrderId}
        /// Sau khi customer send SalesOrder nếu không đủ số lượng hàng sẽ bị reject
        /// <param name="salesOrderId"></param>
        /// <returns></returns>
        [HttpPost("reject/{salesOrderId}")]
        [Authorize(Roles = UserRoles.SALES_STAFF)]
        public async Task<IActionResult> RejectOrder(RejectSalesOrderRequestDTO request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }
            var result = await _service.RejectSalesOrderAsync(request, currentUserId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// POST: http://localhost:5137/api/SalesOrder/confirm-payment
        /// (Xác nhận THỦ CÔNG) đổi trạng thái Pending -> Deposited/Paid (Deposited = 4,Paid = 5,).
        /// </summary>
        [HttpPost("confirm-payment")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> ConfirmPaymentManual(int orderId, PaymentStatus status)
        {
            var result = await _service.ConfirmPaymentAsync(orderId, status);
            if (result.Success)
            {
                await _service.RecalculateTotalReceiveAsync();
            }
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137/api/SalesOrder/details/{orderId}
        /// Lấy chi tiết sales order
        /// </summary>
        [HttpGet("details/{orderId}")]
        [Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.SALES_STAFF + "," + UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> GetDetails(int orderId)
        {
            var result = await _service.GetOrderDetailsAsync(orderId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// GET: http://localhost:5137/api/SalesOrder/my-list-sales-order
        /// Trả về danh sách SalesOrder theo user hiện tại (customer chỉ thấy đơn của mình).
        /// </summary>
        [HttpGet("my-list-sales-order")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> ListMySalesOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _service.ListCustomerSalesOrdersAsync(userId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137/api/SalesOrder/list-sales-order
        /// Trả về danh sách SalesOrder theo user hiện tại (customer chỉ thấy đơn của mình).
        /// </summary>
        [HttpGet("list-sales-order")]
        [Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> ListSalesOrders()
        {
            var result = await _service.ListSalesOrdersAsync();
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: http://localhost:5137/api/SalesOrder/complete/{orderId}
        /// Customer đánh dấu hoàn tất đơn (chỉ khi đã thanh toán và nhận được hàng).
        /// </summary>
        [HttpPost("complete/{orderId}")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> MarkComplete(int orderId)
        {
            var result = await _service.MarkCompleteAsync(orderId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: http://localhost:5137/api/SalesOrder/draft/create?salesQuotationId=123
        /// Tạo SalesOrder trạng thái Draft từ SalesQuotation.
        /// </summary>
        [HttpPost("draft/create")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> CreateDraftFromSalesQuotation([FromBody] SalesOrderRequestDTO body)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            body.CreateBy = userId;

            var result = await _service.CreateDraftFromSalesQuotationAsync(body);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// PUT: http://localhost:5137/api/SalesOrder/draft/{orderId}/quantities
        /// Cập nhật số lượng từng sản phẩm trong Draft (chỉ thay đổi Quantity).
        /// </summary>
        [HttpPut("draft/{orderId}/quantities")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> UpdateDraftQuantities([FromBody] SalesOrderUpdateDTO items)
        {
            var result = await _service.UpdateDraftQuantitiesAsync(items);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// DELETE: http://localhost:5137/api/SalesOrder/draft/{orderId}
        /// Xoá SalesOrder khi còn ở trạng thái Draft.
        /// </summary>
        [HttpDelete("draft/{orderId}")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> DeleteDraft(int orderId)
        {
            var result = await _service.DeleteDraftAsync(orderId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: http://localhost:5137/api/SalesOrder/total-receipt
        /// </summary>
        /// 
        /// <returns></returns>
        [HttpPost("total-receipt")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> RecalculateTotalReceive()
        {
            var result = await _service.RecalculateTotalReceiveAsync();

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137/api/SalesOrder/list-sales-order-not-delivered
        /// </summary>
        /// <returns></returns>
        [HttpGet("list-sales-order-not-delivered")]
        public async Task<IActionResult> ListSalesOrderNotDelivered()
        {
            var result = await _service.ListSaleOrderNotDeliveredAsync();

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// http://localhost:5137/api/SalesOrder/check-delivered-sales-order
        /// </summary>
        /// <param name="SalesOrderId"></param>
        /// <returns></returns>
        [HttpPost("check-delivered-sales-order")]
        public async Task<IActionResult> checkDeliveredSalesOrder()
        {
            var result = await _service.CheckAndUpdateDeliveredStatusAsync();

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// PUT: api/SalesOrder/{salesOrderId}/mark-backorder
        /// Nếu tồn tại StockExportOrder có trạng thái NotEnough thì chuyển SalesOrder sang BackSalesOrder.
        /// </summary>
        [HttpPut("{salesOrderId}/mark-backorder")]
        [Authorize(Roles = UserRoles.SALES_STAFF)]
        public async Task<IActionResult> MarkBackSalesOrder(int salesOrderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _service.MarkBackSalesOrderAsync(salesOrderId, userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// PUT: api/SalesOrder/{salesOrderId}/mark-not-complete
        /// Chuyển SalesOrder sang NotComplete và chuyển trạng thái thanh toán thành Refunded.
        /// </summary>
        [HttpPut("{salesOrderId}/mark-not-complete")]
        [Authorize(Roles = UserRoles.SALES_STAFF)]
        public async Task<IActionResult> MarkNotComplete(int salesOrderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _service.MarkNotCompleteAndRefundAsync(salesOrderId, userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        #region salesOrderStatistics

        /// <summary>
        /// GET  https://api.bbpharmacy.site/api/SalesOrder/revenue/{year}
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet("revenue/{year}")]
        [Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetYearRevenue(int year)
        {
            var result = await _service.GetYearRevenueAsync(year);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET https://api.bbpharmacy.site/api/SalesOrder/sales-product-quantity/{year}
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet("sales-product-quantity/{year}")]
        [Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetProductQuantityByYear(int year)
        {
            var result = await _service.GetProductQuantityByYearAsync(year);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        #endregion

        #region depositmanual

        /// <summary>
        /// Customer tạo yêu cầu check cọc manual
        /// POST: http://localhost:5137/api/SalesOrder/{salesOrderId}/deposit-checks/manual
        /// <param name="salesOrderId"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("{salesOrderId}/deposit-checks/manual")]
        [Authorize(Roles = UserRoles.CUSTOMER)] 
        public async Task<IActionResult> CreateManualDepositCheck(int salesOrderId,[FromBody] CreateSalesOrderDepositCheckRequestDTO dto)
        {
            dto.SalesOrderId = salesOrderId;
            var userId = GetUserId();

            var result = await _service.CreateDepositCheckRequestAsync(dto, userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Accountant approve request check cọc manual của customer
        /// POST: http://localhost:5137/api/SalesOrder/deposit-checks/{requestId}/approve
        /// <param name="requestId"></param>
        /// <returns></returns>
        [HttpPost("deposit-checks/{requestId}/approve")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> ApproveManualDepositCheck(int requestId)
        {
            var accountantId = GetUserId();
            var result = await _service.ApproveDepositCheckAsync(requestId, accountantId);
            if (result.Success)
            {
                await _service.RecalculateTotalReceiveAsync();
            }
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Accountant reject request check cọc manual của customer
        /// POST: http://localhost:5137/api/SalesOrder/deposit-checks/{requestId}/reject
        /// <param name="requestId"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("deposit-checks/{requestId}/reject")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> RejectManualDepositCheck(
            int requestId,
            [FromBody] RejectSalesOrderDepositCheckDTO dto)
        {
            dto.RequestId = requestId;
            var accountantId = GetUserId();
            var result = await _service.RejectDepositCheckAsync(dto, accountantId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// CUSTOMER: list tất cả manual deposit check của mình
        /// GET: http://localhost:5137/api/SalesOrder/deposit-checks/manual/my
        /// <returns></returns>
        [HttpGet("deposit-checks/manual/my")]
        [Authorize]
        public async Task<IActionResult> GetMyManualDepositChecks()
        {
            var userId = GetUserId();
            var result = await _service.ListDepositChecksForCustomerAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// CUSTOMER: xem chi tiết 1 request
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        [HttpGet("deposit-checks/manual/{requestId:int}")]
        [Authorize]
        public async Task<IActionResult> GetManualDepositCheckDetail(int requestId)
        {
            var userId = GetUserId();
            var result = await _service.GetDepositCheckDetailForCustomerAsync(requestId, userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("deposit-checks/manual/{requestId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateManualDepositCheck(
            int requestId,
            [FromBody] UpdateSalesOrderDepositCheckRequestDTO dto)
        {
            var userId = GetUserId();
            var result = await _service.UpdateDepositCheckRequestAsync(requestId, userId, dto);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// CUSTOMER: xoá request nếu vẫn Pending
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        [HttpDelete("deposit-checks/manual/{requestId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteManualDepositCheck(int requestId)
        {
            var userId = GetUserId();
            var result = await _service.DeleteDepositCheckRequestAsync(requestId, userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Accountant list các yêu cầu kiểm tra cọc manual.
        /// GET: http://localhost:5137/api/SalesOrder/all-deposit-checks/manual
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet("all-deposit-checks/manual")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> GetManualDepositChecks([FromQuery] DepositCheckStatus? status)
        {
            var result = await _service.ListDepositChecksAsync(status);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
        #endregion

    }
}
