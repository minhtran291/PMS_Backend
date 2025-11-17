
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMS.Application.DTOs.VNPay;
using QRCoder;
using System.Globalization;
using System.Web;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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

        // VNPay encoder: UrlEncode(UTF8) => spaces "+" => HEX uppercase
        private static string VnPayEnc(string? value)
        {
            var s = HttpUtility.UrlEncode(value ?? string.Empty, Encoding.UTF8) ?? string.Empty;
            if (s.Length == 0) return string.Empty;

            // Theo sample VNPay: dùng "+" cho space
            s = s.Replace("%20", "+");

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c == '%' && i + 2 < s.Length)
                {
                    sb.Append('%');
                    sb.Append(char.ToUpperInvariant(s[i + 1]));
                    sb.Append(char.ToUpperInvariant(s[i + 2]));
                    i += 2;
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// BỎ DẤU + chỉ giữ A-Z, a-z, 0-9 và space
        /// để đáp ứng yêu cầu: vnp_OrderInfo Alphanumeric, không dấu, không ký tự đặc biệt.
        /// </summary>
        private static string SanitizeOrderInfo(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // 1. Bỏ dấu tiếng Việt
            string formD = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);

            // 2. Chỉ giữ A-Z, a-z, 0-9 và khoảng trắng
            var clean = Regex.Replace(noDiacritics, @"[^A-Za-z0-9 ]", string.Empty);

            // 3. Gộp space thừa
            return Regex.Replace(clean, @"\s+", " ").Trim();
        }

        public async Task<(string url, string qrBase64, string txnRef)> BuildPaymentAsync(
            int salesOrderId, decimal amount, string orderInfo, string? locale, string clientIp)
        {
            var amountVnp = ((long)(amount * 100m)).ToString(CultureInfo.InvariantCulture);

            var tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(tzId));

            var txnRef = $"{salesOrderId}-{nowVn:yyyyMMddHHmmssfff}";

            var ip = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp;
            if (ip == "::1") ip = "127.0.0.1";

            // ✅ SỬA: OrderInfo gửi lên VNPay phải được sanitize
            var safeOrderInfo = SanitizeOrderInfo(orderInfo);

            var p = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _opt.TmnCode,
                ["vnp_Amount"] = amountVnp,
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = txnRef,
                ["vnp_OrderInfo"] = safeOrderInfo,   // ✅ DÙNG safeOrderInfo
                ["vnp_OrderType"] = "billpayment",
                ["vnp_ReturnUrl"] = _opt.ReturnUrl,
                ["vnp_IpAddr"] = ip,
                ["vnp_CreateDate"] = nowVn.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = nowVn.AddMinutes(15).ToString("yyyyMMddHHmmss")
            };

            if (!string.IsNullOrWhiteSpace(locale) && !string.Equals(locale, "null", StringComparison.OrdinalIgnoreCase))
                p["vnp_Locale"] = locale.Trim();

            // HashData string: SORT ALPHABET + URL ENCODE (VnPayEnc)
            var rawForHash = string.Join("&", p.Select(kv => $"{kv.Key}={VnPayEnc(kv.Value)}"));
            var secureHash = HmacSHA512(_opt.HashSecret, rawForHash).ToLowerInvariant();

            // Không thêm vnp_SecureHashType (optional, và không nằm trong chuỗi ký)
            var payUrl = $"{_opt.VnpUrl}?{rawForHash}&vnp_SecureHash={secureHash}";

            string qrBase64;
            using (var qrGen = new QRCodeGenerator())
            using (var data = qrGen.CreateQrCode(payUrl, QRCodeGenerator.ECCLevel.M))
            using (var qr = new PngByteQRCode(data))
            {
                var bytes = qr.GetGraphic(5);
                qrBase64 = "data:image/png;base64," + Convert.ToBase64String(bytes);
            }

            _logger.LogInformation("VNPay REQUEST RAW = {raw}", rawForHash);
            _logger.LogInformation("VNPay REQUEST HASH = {hash}", secureHash);

            return (payUrl, qrBase64, txnRef);
        }

        public string? GetQueryValue(IQueryCollection query, string key)
            => query.TryGetValue(key, out var val) ? val.ToString() : null;

        private static string HmacSHA512(string? key, string? data)
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
                var k = kv.Key;
                if (k.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                    !k.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                    !k.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    var val = kv.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(val) && !string.Equals(val, "null", StringComparison.OrdinalIgnoreCase))
                        input[k] = val;
                }
            }

            // ✅ Hash lại y chang rule build request:
            // sort alphab + VnPayEnc (UrlEncode UTF8 + "+" cho space + HEX uppercase)
            var raw = string.Join("&", input.Select(kv => $"{kv.Key}={VnPayEnc(kv.Value)}"));
            var myHash = HmacSHA512(_opt.HashSecret, raw).ToLowerInvariant();
            var vnpHash = query["vnp_SecureHash"].ToString();

            _logger.LogInformation("VNPay RETURN/IPN RAW = {raw}", raw);
            _logger.LogInformation("VNPay RETURN/IPN MYHASH = {hash}, VNP_HASH = {vnp}", myHash, vnpHash);

            var ok = string.Equals(myHash, vnpHash, StringComparison.OrdinalIgnoreCase);
            if (!ok)
                _logger.LogWarning("VNPay signature invalid.");

            return ok;
        }
    }
}
