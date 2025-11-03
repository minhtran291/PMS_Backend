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
        /// GET: https://localhost:7213/api/SalesOrder/quotation-products/{qid}
        /// Lấy danh sách sản phẩm thuộc Quotation được chỉ định.
        /// </summary>
        [HttpGet("quotation-products/{qid}")]
        public async Task<IActionResult> GetQuotationProducts(int qid)
        {
            var result = await _service.GetQuotationProductsAsync(qid);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/fefo-plan
        /// Nhập danh sách {productId, quantity} để tính FEFO theo LotProduct.
        /// </summary>
        [HttpPost("fefo-plan")]
        public async Task<IActionResult> BuildFefoPlan([FromBody] FEFOPlanRequestDTO request)
        {
            var result = await _service.BuildFefoPlanAsync(request);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/create
        /// Tạo SalesOrder ở trạng thái Pending từ kết quả FEFO (chưa trừ kho).
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = UserRoles.CUSTOMER)]
        public async Task<IActionResult> CreateOrder([FromBody] FEFOPlanRequestDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _service.CreateOrderFromQuotationAsync(request, createdBy: userId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: https://localhost:7213/api/SalesOrder/confirm-payment
        /// (Xác nhận THỦ CÔNG) đổi trạng thái Pending -> Deposited/Paid và trừ kho.
        /// </summary>
        [HttpPost("confirm-payment")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.SALES_STAFF)]
        public async Task<IActionResult> ConfirmPaymentManual(string orderId, decimal amountVnd, string method = "Manual", string? txnId = null)
        {
            var result = await _service.ConfirmPaymentAsync(orderId, amountVnd, method, txnId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: https://localhost:7213/api/SalesOrder/details/{orderId}
        /// </summary>
        [HttpGet("details/{orderId}")]
        public async Task<IActionResult> GetDetails(string orderId)
        {
            var result = await _service.GetOrderDetailsAsync(orderId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
    }
}
