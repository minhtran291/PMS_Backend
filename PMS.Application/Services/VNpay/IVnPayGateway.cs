using Microsoft.AspNetCore.Http;

namespace PMS.API.Helpers.VnPay
{
    public interface IVnPayGateway
    {
        // Build payment URL & QR cho VNPay
        Task<(string url, string qrBase64, string txnRef)> BuildPaymentAsync(
        int salesOrderId, decimal amount, string orderInfo, string? locale, string clientIp);

        // Xác thực chữ ký (vnp_SecureHash) từ VNPay trả về (Return/IPN)
        bool ValidateSignature(IQueryCollection query);
        string? GetQueryValue(IQueryCollection query, string key);
    }
}
