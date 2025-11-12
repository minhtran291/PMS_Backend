
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMS.Application.DTOs.VNPay;
using QRCoder;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PMS.API.Helpers.VnPay
{
    public sealed class VnPayGateway : IVnPayGateway
    {

        private readonly VNPayConfig _opt;
        private readonly ILogger<VnPayGateway> _logger;
        public VnPayGateway(IOptions<VNPayConfig> opt, ILogger<VnPayGateway> logger)
        {
            _opt = new VNPayConfig
            {
                TmnCode = opt.Value.TmnCode?.Trim() ?? "",
                HashSecret = opt.Value.HashSecret?.Trim() ?? "",
                VnpUrl = opt.Value.VnpUrl?.Trim() ?? "",
                ReturnUrl = opt.Value.ReturnUrl?.Trim() ?? "",
                IpnDebugDomain = opt.Value.IpnDebugDomain?.Trim() ?? ""
            };
            _logger = logger;
        }

        public async Task<(string url, string qrBase64, string txnRef)> BuildPaymentAsync(int salesOrderId, decimal amount, string orderInfo, string? bankCode, string? locale, string clientIp)
        {
            // VNPay yêu cầu VND *100
            var amountVnp = ((long)(amount * 100m)).ToString(CultureInfo.InvariantCulture);

            // Giờ Việt Nam (UTC+7) cho Create/Expire
            var tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(tzId));

            // TxnRef phải duy nhất
            var txnRef = $"{salesOrderId}-{nowVn:yyyyMMddHHmmssfff}";

            // Gom params (chỉ add param có giá trị)
            var p = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _opt.TmnCode,
                ["vnp_Amount"] = amountVnp,
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = txnRef,
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "billpayment",
                ["vnp_ReturnUrl"] = _opt.ReturnUrl,
                ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
                ["vnp_CreateDate"] = nowVn.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = nowVn.AddMinutes(15).ToString("yyyyMMddHHmmss")
            };
            if (!string.IsNullOrWhiteSpace(locale)) p["vnp_Locale"] = locale!.Trim();
            if (!string.IsNullOrWhiteSpace(bankCode)) p["vnp_BankCode"] = bankCode!.Trim();

            // Encode giá trị thống nhất cho cả ký & URL
            static string Enc(string s) => Uri.EscapeDataString(s);

            // Raw string để KÝ (không gồm vnp_SecureHash/_Type)
            var rawForHash = string.Join("&", p.Select(kv => $"{kv.Key}={Enc(kv.Value)}"));

            var secureHash = HmacSHA512(_opt.HashSecret, rawForHash);

            // Build URL cuối
            var payUrl = $"{_opt.VnpUrl}?{rawForHash}&vnp_SecureHash={secureHash}";

            // Sinh QR từ payment URL (tuỳ chọn)
            string qrBase64;
            using (var qrGen = new QRCodeGenerator())
            using (var data = qrGen.CreateQrCode(payUrl, QRCodeGenerator.ECCLevel.M))
            using (var qr = new PngByteQRCode(data))
            {
                var bytes = qr.GetGraphic(5);
                qrBase64 = "data:image/png;base64," + Convert.ToBase64String(bytes);
            }

            // Log để tự kiểm tra khi cần
            _logger.LogInformation("VNPay RAW = {raw}", rawForHash);
            _logger.LogInformation("VNPay HASH = {hash}", secureHash);

            return (payUrl, qrBase64, txnRef);
        }

        public string? GetQueryValue(IQueryCollection query, string key) => query.TryGetValue(key, out var val) ? val.ToString() : null;

        private static string HmacSHA512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key ?? "");
            var dataBytes = Encoding.UTF8.GetBytes(data ?? "");
            using var h = new HMACSHA512(keyBytes);
            var hash = h.ComputeHash(dataBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }


        public bool ValidateSignature(IQueryCollection query)
        {
            var input = new SortedDictionary<string, string>();
            foreach (var kv in query)
            {
                var key = kv.Key;
                if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    input[key] = kv.Value.ToString();
                }
            }

            static string Enc(string s) => Uri.EscapeDataString(s);
            var raw = string.Join("&", input.Select(kv => $"{kv.Key}={Enc(kv.Value)}"));
            var myHash = HmacSHA512(_opt.HashSecret, raw);
            var vnpHash = query["vnp_SecureHash"].ToString();

            var ok = string.Equals(myHash, vnpHash, StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                _logger.LogWarning("VNPay signature invalid. RAW={raw}, MY={my}, VNP={vnp}", raw, myHash, vnpHash);
            }
            return ok;
        }
    }
}
