using MailKit.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using PMS.Application.DTOs.VNPay;
using System;
using QRCoder;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.VNpay
{
    public class VnPayService : IVnPayService
    {
        private readonly VNPayConfig _opt;
        public VnPayService(IOptions<VNPayConfig> opt) => _opt = opt.Value;

        public string CreatePaymentUrl(string salesOrderId, long amountVnd, string orderInfo, string locale = "vn")
        {
            var dict = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _opt.TmnCode,
                ["vnp_Amount"] = (amountVnd * 100).ToString(), // VNPay yêu cầu *100
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = salesOrderId,
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = locale,
                ["vnp_ReturnUrl"] = _opt.ReturnUrl,
                ["vnp_IpnUrl"] = _opt.IpnDebugDomain,          // IPN gửi về API này
                ["vnp_IpAddr"] = "127.0.0.1",
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss")
            };

            // Ký chữ ký trên chuỗi raw KHÔNG có vnp_SecureHash
            var raw = BuildQuery(dict);
            var sign = ComputeHmacSHA512(_opt.HashSecret, raw);

            // Thêm chữ ký rồi dùng AddQueryString để ghép URL
            dict["vnp_SecureHash"] = sign;

            return QueryHelpers.AddQueryString(_opt.VnpUrl, dict);
        }

        public string GenerateQrDataUrl(string paymentUrl)
        {
            using var gen = new QRCodeGenerator();
            var data = gen.CreateQrCode(paymentUrl, QRCodeGenerator.ECCLevel.Q);
            using var qr = new QRCode(data);
            using var bmp = qr.GetGraphic(5);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
        }

        public bool ValidateReturn(IQueryCollection query, out IDictionary<string, string> data)
        {
            var dict = new SortedDictionary<string, string>();
            foreach (var kv in query)
                if (kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                    dict[kv.Key] = kv.Value!;

            var raw = BuildQuery(dict);
            var calc = ComputeHmacSHA512(_opt.HashSecret, raw);

            data = dict;
            var received = query["vnp_SecureHash"].ToString();
            return string.Equals(calc, received, StringComparison.OrdinalIgnoreCase);
        }

        // Helpers
        private static string BuildQuery(IDictionary<string, string> dict)
        {
            var sb = new StringBuilder();
            foreach (var kv in dict.OrderBy(k => k.Key))
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
            }
            return sb.ToString();
        }

        private static string ComputeHmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return string.Concat(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)).Select(b => b.ToString("x2")));
        }
    }
}