using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMS.Application.DTOs.Invoice;
using PMS.Core.ConfigOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PMS.Application.Services.SmartCA
{
    public class SmartCAService : ISmartCAService
    {
        private readonly HttpClient _httpClient;
        private readonly SmartCAConfig _opt;
        private readonly ILogger<SmartCAService> _logger;

        public SmartCAService(
        HttpClient httpClient,
        IOptions<SmartCAConfig> opt,
        ILogger<SmartCAService> logger)
        {
            _httpClient = httpClient;
            _opt = opt.Value;
            _logger = logger;
        }

        private static string ComputeSha256Hex(byte[] data)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2")); // lowercase hex
            return sb.ToString();
        }

        public async Task<SmartCASignResult> SignPdfHashAsync(byte[] pdfBytes, string docId, SmartCASignInvoiceRequestDTO userInfo, CancellationToken cancellationToken = default)
        {
            var hashHex = ComputeSha256Hex(pdfBytes);

            var transactionId = $"SP_CA_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}";

            var payload = new
            {
                sp_id = _opt.SpId,
                sp_password = _opt.SpPassword,
                user_id = userInfo.UserId,
                password = userInfo.Password,
                otp = userInfo.Otp,
                transaction_id = transactionId,
                transaction_desc = docId,
                sign_files = new[]
                {
                new
                {
                    file_type = "pdf",
                    data_to_be_signed = hashHex,
                    doc_id = docId,
                    sign_type = "hash"
                }
            },
                time_stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_opt.BaseUrl}/v2/signatures/sign";
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SmartCA sign error: {Status} {Body}",
                    response.StatusCode, body);
                throw new Exception($"SmartCA sign error: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var statusCode = root.GetProperty("status_code").GetInt32();
            if (statusCode != 200)
            {
                var message = root.GetProperty("message").GetString();
                throw new Exception($"SmartCA sign error: {statusCode} - {message}");
            }

            var data = root.GetProperty("data");
            return new SmartCASignResult
            {
                TransactionId = data.GetProperty("transaction_id").GetString()!,
                TranCode = data.GetProperty("tran_code").GetString()!
            };
        }
    }
}
