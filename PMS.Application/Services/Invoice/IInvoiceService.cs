using PMS.Application.DTOs.Invoice;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.Invoice
{
    public interface IInvoiceService
    {
        Task<ServiceResult<InvoiceDTO>>
            GenerateInvoiceFromPaymentRemainsAsync(GenerateInvoiceFromPaymentRemainsRequestDTO request);

        Task<ServiceResult<InvoicePDFResultDTO>> GenerateInvoicePdfAsync(int invoiceId);
        Task<ServiceResult<bool>> SendInvoiceEmailAsync(int invoiceId);
    }
}
