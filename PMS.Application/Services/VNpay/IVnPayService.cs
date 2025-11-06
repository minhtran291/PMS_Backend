using Microsoft.AspNetCore.Http;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.VNpay
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(string salesOrderId,
            long amountVnd, string orderInfo, string locale = "vn");
        bool ValidateReturn(IQueryCollection query, out IDictionary<string, string> data);
        string GenerateQrDataUrl(string paymentUrl);
        Task<ServiceResult<bool>> VNPayConfirmPaymentAsync(string salesOrderId, decimal amountVnd,
            string gateway, string? externalTxnId);
    }
}
