using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.VnPay;
using PMS.Application.Services.SalesOrder;
using PMS.Application.Services.VNpay;
using PMS.Core.Domain.Constant;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IVnPayService _vnPay;
        private readonly ISalesOrderService _salesOrder;

        public PaymentController(IVnPayService vnPayService, ISalesOrderService salesOrderService)
        {
            _vnPay = vnPayService;
            _salesOrder = salesOrderService;
        }

        /// <summary>
        /// Post: http://localhost:5137/api/Payment/init
        /// Khởi tạo thanh toán VNPay (deposit/full). Trả về link và QR.
        /// </summary>
        [HttpPost("init")]
        public async Task<IActionResult> Init([FromBody] VnPayInitRequestDTO req)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _vnPay.InitVnPayAsync(req, clientIp);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Get: http://localhost:5137/api/Payment/return
        /// ReturnUrl cho web (VNPay redirect sau khi thanh toán).
        /// </summary>
        [HttpGet("return")]
        public async Task<IActionResult> Return()
        {
            var result = await _vnPay.HandleVnPayReturnAsync(Request.Query);
            // return Redirect($"https://your-frontend.com/payment/result?success={rs.Data}");
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// IPN (Instant Payment Notification) - server to server.
        /// VNPay sẽ gọi endpoint này để confirm trạng thái thanh toán.
        /// Get: http://localhost:5137/api/Payment/vnpay/ipn
        /// </summary>
        [HttpGet("vnpay/ipn")]
        public async Task<IActionResult> VnPayIpn()
        {
            var (ok, code, msg) = await _vnPay.HandleVnPayIpnAsync(Request.Query)
                .ContinueWith(t =>
                {
                    var r = t.Result;
                    // Map sang code VNPay:
                    // 00 = nhận thành công; 97 = chữ ký sai; 04 = đã xác nhận; 99 = lỗi khác
                    if (!r.Success && r.StatusCode == 400 && r.Message.Contains("Chữ ký")) return (false, "97", "Invalid signature");
                    if (!r.Success && r.StatusCode == 400) return (false, "99", r.Message);
                    if (!r.Success && r.StatusCode == 404) return (false, "01", r.Message);
                    return (true, "00", "Confirm Success");
                });

            return Content($"RspCode={code}&Message={msg}", "text/plain");
        }

    }
}
