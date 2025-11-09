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

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/fefo-plan
        /// Nhập danh sách {productId, quantity} để tính FEFO theo LotProduct.
        /// </summary>
        //[HttpPost("fefo-plan")]
        //public async Task<IActionResult> BuildFefoPlan([FromBody] FEFOPlanRequestDTO request)
        //{
        //    //var result = await _service.BuildFefoPlanAsync(request);
        //    return StatusCode(200, new
        //    {
        //        message = "",
        //    });
        //}

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
        /// POST: https://localhost:7213/api/SalesOrder/send/{salesOrderId}
        /// Customer gửi đơn (Draft -> Send). Hệ thống kiểm tra tồn kho và cảnh báo
        /// tới PURCHASES_STAFF nếu thiếu/sắp hết.
        /// </summary>
        [HttpPost("send/{salesOrderId}")]
        //[Authorize(Roles = UserRoles.CUSTOMER)]
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
        /// POST: https://localhost:7213/api/SalesOrder/approve/{salesOrderId}
        /// Sau khi customer send SalesOrder nếu đủ số lượng hàng sẽ được approved
        /// <param name="salesOrderId"></param>
        /// <returns></returns>
        [HttpPost("approve/{salesOrderId}")]
        //[Authorize(Roles = UserRoles.SALES_STAFF)]
        public async Task<IActionResult> ApproveOrder(int salesOrderId)
        {
            var result = await _service.ApproveSalesOrderAsync(salesOrderId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/reject/{salesOrderId}
        /// Sau khi customer send SalesOrder nếu không đủ số lượng hàng sẽ bị reject
        /// <param name="salesOrderId"></param>
        /// <returns></returns>
        [HttpPost("reject/{salesOrderId}")]
        //[Authorize(Roles = UserRoles.SALES_STAFF)]
        public async Task<IActionResult> RejectOrder(int salesOrderId)
        {
            var result = await _service.RejectSalesOrderAsync(salesOrderId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/confirm-payment
        /// (Xác nhận THỦ CÔNG) đổi trạng thái Pending -> Deposited/Paid (Deposited = 4,Paid = 5,).
        /// </summary>
        [HttpPost("confirm-payment")]
        //[Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.SALES_STAFF)]
        public async Task<IActionResult> ConfirmPaymentManual(int orderId, SalesOrderStatus status)
        {
            var result = await _service.ConfirmPaymentAsync(orderId, status);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: https://localhost:7213/api/SalesOrder/details/{orderId}
        /// Lấy chi tiết sales order
        /// </summary>
        [HttpGet("details/{orderId}")]
        //[Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.SALES_STAFF)]
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
        /// GET: https://localhost:7213/api/SalesOrder/my-list-sales-order
        /// Trả về danh sách SalesOrder theo user hiện tại (customer chỉ thấy đơn của mình).
        /// </summary>
        [HttpGet("my-list-sales-order")]
        //[Authorize(Roles = UserRoles.CUSTOMER)]
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
        /// GET: https://localhost:7213/api/SalesOrder/list-sales-order
        /// Trả về danh sách SalesOrder theo user hiện tại (customer chỉ thấy đơn của mình).
        /// </summary>
        [HttpGet("list-sales-order")]
        //[Authorize]
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
        /// POST: https://localhost:7213/api/SalesOrder/complete/{orderId}
        /// Customer đánh dấu hoàn tất đơn (chỉ khi đã thanh toán và nhận được hàng).
        /// </summary>
        [HttpPost("complete/{orderId}")]
        //[Authorize(Roles = UserRoles.CUSTOMER)]
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
        /// POST: https://localhost:7213/api/SalesOrder/draft/create?salesQuotationId=123
        /// Tạo SalesOrder trạng thái Draft từ SalesQuotation.
        /// </summary>
        [HttpPost("draft/create")]
        //[Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> CreateDraftFromSalesQuotation([FromBody] SalesOrderRequestDTO body)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            body.CreateBy = userId;
            body.IsDeposited = false;
            body.Status = SalesOrderStatus.Draft;

            var result = await _service.CreateDraftFromSalesQuotationAsync(body);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// PUT: https://localhost:7213/api/SalesOrder/draft/{orderId}/quantities
        /// Cập nhật số lượng từng sản phẩm trong Draft (chỉ thay đổi Quantity).
        /// </summary>
        //[HttpPut("draft/{orderId}/quantities")]
        //[Authorize(Roles = UserRoles.CUSTOMER)]
        //public async Task<IActionResult> UpdateDraftQuantities(string orderId, [FromBody] List<DraftSalesOrderDTO> items)
        //{
        //    //var result = await _service.UpdateDraftQuantitiesAsync(orderId, items);
        //    return StatusCode(200, new
        //    {
        //        message = "",
        //    });
        //}

        /// <summary>
        /// DELETE: https://localhost:7213/api/SalesOrder/draft/{orderId}
        /// Xoá SalesOrder khi còn ở trạng thái Draft.
        /// </summary>
        [HttpDelete("draft/{orderId}")]
        //[Authorize(Roles = UserRoles.CUSTOMER)]
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
    }
}
