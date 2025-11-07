using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.Services.SalesOrder;
using PMS.Application.Services.VNpay;
using PMS.Core.Domain.Constant;
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
        [HttpPost("fefo-plan")]
        public async Task<IActionResult> BuildFefoPlan([FromBody] FEFOPlanRequestDTO request)
        {
            //var result = await _service.BuildFefoPlanAsync(request);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/send/{orderId}
        /// Customer gửi đơn (Draft -> Send). Hệ thống kiểm tra tồn kho và cảnh báo
        /// tới PURCHASES_STAFF nếu thiếu/sắp hết.
        /// </summary>
        [HttpPost("send/{orderId}")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> SendOrder(string orderId)
        {
            //var result = await _service.SendOrderAsync(orderId);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/confirm-payment
        /// (Xác nhận THỦ CÔNG) đổi trạng thái Pending -> Deposited/Paid và trừ kho.
        /// </summary>
        [HttpPost("confirm-payment")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.SALES_STAFF)]
        public async Task<IActionResult> ConfirmPaymentManual(string orderId)
        {
            //var result = await _service.ConfirmPaymentAsync(orderId);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// GET: https://localhost:7213/api/SalesOrder/details/{orderId}
        /// Lấy chi tiết sales order
        /// </summary>
        [HttpGet("details/{orderId}")]
        public async Task<IActionResult> GetDetails(string orderId)
        {
            //var result = await _service.GetOrderDetailsAsync(orderId);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// GET: https://localhost:7213/api/SalesOrder/list
        /// Trả về danh sách SalesOrder theo user hiện tại (customer chỉ thấy đơn của mình).
        /// </summary>
        [HttpGet("list")]
        [Authorize]
        public async Task<IActionResult> ListMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            //var result = await _service.ListOrdersAsync(userId);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/complete/{orderId}
        /// Customer đánh dấu hoàn tất đơn (chỉ khi đã thanh toán và nhận được hàng).
        /// </summary>
        [HttpPost("complete/{orderId}")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> MarkComplete(string orderId)
        {
            //var result = await _service.MarkCompleteAsync(orderId);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/draft/create?salesQuotationId=123
        /// Tạo SalesOrder trạng thái Draft từ SalesQuotation.
        /// </summary>
        [HttpPost("draft/create")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> CreateDraftFromSalesQuotation([FromQuery] int salesQuotationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            //var result = await _service.CreateDraftFromSalesQuotationAsync(salesQuotationId, userId);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// PUT: https://localhost:7213/api/SalesOrder/draft/{orderId}/quantities
        /// Cập nhật số lượng từng sản phẩm trong Draft (chỉ thay đổi Quantity).
        /// </summary>
        [HttpPut("draft/{orderId}/quantities")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> UpdateDraftQuantities(string orderId, [FromBody] List<DraftSalesOrderDTO> items)
        {
            //var result = await _service.UpdateDraftQuantitiesAsync(orderId, items);
            return StatusCode(200, new
            {
                message = "",
            });
        }

        /// <summary>
        /// DELETE: https://localhost:7213/api/SalesOrder/draft/{orderId}
        /// Xoá SalesOrder khi còn ở trạng thái Draft.
        /// </summary>
        [HttpDelete("draft/{orderId}")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> DeleteDraft(string orderId)
        {
            //var result = await _service.DeleteDraftAsync(orderId);
            return StatusCode(200, new
            {
                message = "",
            });
        }
    }
}
