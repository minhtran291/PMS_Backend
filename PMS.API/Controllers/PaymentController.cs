using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.SalesOrder;
using PMS.Application.Services.VNpay;
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
        /// POST: https://localhost:7213/api/VNPay/create-payment
        /// Tạo link và QR thanh toán cho một order (deposit/full).
        /// </summary>
        /// <param name="orderId">Mã đơn hàng</param>
        /// <param name="paymentType">"deposit" hoặc "full"</param>
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment(string orderId, string paymentType = "deposit")
        {
            var details = await _salesOrder.GetOrderDetailsAsync(orderId);
            if (!details.Success || details.Data == null)
            {
                return StatusCode(details.StatusCode, new
                {
                    success = details.Success,
                    message = details.Message,
                    data = details.Data
                });
            }

            // Lấy total từ payload (anonymous object) => dynamic
            dynamic dto = details.Data!;
            decimal total = dto.OrderTotalPrice;

            decimal amount = paymentType.Equals("deposit", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(total / 10m, 0) 
                : total;

            var orderInfo = paymentType.Equals("deposit", StringComparison.OrdinalIgnoreCase)
                ? $"Thanh toán cọc đơn {orderId}"
                : $"Thanh toán toàn bộ đơn {orderId}";

            var url = _vnPay.CreatePaymentUrl(orderId, (long)amount, orderInfo);
            var qr = _vnPay.GenerateQrDataUrl(url);

            return Ok(new
            {
                success = true,
                message = "Khởi tạo thanh toán VNPay thành công",
                data = new
                {
                    orderId,
                    paymentType,
                    amount,
                    paymentUrl = url,
                    qrDataUrl = qr
                }
            });
        }

        /// <summary>
        /// GET: https://localhost:7213/api/VNPay/ipn
        /// Nhận IPN từ VNPay
        /// Thành công sẽ tự động ConfirmPayment và trừ kho.
        /// </summary>
        [HttpGet("ipn")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> InstantPaymentNotification()
        {
            if (!_vnPay.ValidateReturn(Request.Query, out var data))
            {
                return BadRequest(new { success = false, message = "Chữ ký VNPay không hợp lệ." });
            }

            var orderId = data.ContainsKey("vnp_TxnRef") ? data["vnp_TxnRef"] : "";
            var resp = data.ContainsKey("vnp_ResponseCode") ? data["vnp_ResponseCode"] : "";
            var amountRaw = data.ContainsKey("vnp_Amount") ? data["vnp_Amount"] : "0";
            var txn = data.ContainsKey("vnp_BankTranNo") ? data["vnp_BankTranNo"] : null;

            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest(new { success = false, message = "Thiếu mã đơn hàng" });

            if (!long.TryParse(amountRaw, out var amountTimes100)) amountTimes100 = 0;
            decimal amountVnd = amountTimes100 / 100m;

            if (resp == "00") // Thành công
            {
                var result = await _salesOrder.ConfirmPaymentAsync(orderId, amountVnd, "VNPay", txn);
                return StatusCode(result.StatusCode, new
                {
                    success = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }

            // Thất bại/huỷ
            return Ok(new
            {
                success = false,
                message = $"Giao dịch VNPay thất bại (code={resp})",
                data
            });
        }

        /// <summary>
        /// GET: https://localhost:7213/api/VNPay/return
        /// </summary>
        [HttpGet("return")]
        public IActionResult Return()
        {
            if (!_vnPay.ValidateReturn(Request.Query, out var data))
            {
                return BadRequest(new { success = false, message = "Dữ liệu VNPay không hợp lệ." });
            }

            var resp = data.ContainsKey("vnp_ResponseCode") ? data["vnp_ResponseCode"] : "";
            var success = resp == "00";

            return Ok(new
            {
                success,
                message = success ? "Thanh toán thành công" : "Thanh toán thất bại hoặc bị huỷ",
                data
            });
        }
    }
}
