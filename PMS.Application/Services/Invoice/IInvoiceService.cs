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
            GenerateInvoiceFromGINAsync(GenerateInvoiceFromGINRequestDTO request);

        Task<ServiceResult<InvoicePDFResultDTO>> GenerateInvoicePdfAsync(int invoiceId);
        Task<ServiceResult<bool>> SendInvoiceEmailAsync(int invoiceId, string currentUserId);

        Task<ServiceResult<List<InvoiceDTO>>> GetAllInvoicesAsync();
        Task<ServiceResult<InvoiceDTO>> GetInvoiceByIdAsync(int invoiceId);
        Task<ServiceResult<InvoiceDTO>> UpdateInvoiceGoodsIssueNotesAsync(
            int invoiceId,
            InvoiceUpdateDTO request);

        Task<ServiceResult<List<string>>> GetAllSalesOrderCodesAsync();
        Task<ServiceResult<List<string>>> GetGoodsIssueNoteCodesBySalesOrderCodeAsync(string salesOrderCode);
        Task<ServiceResult<List<InvoiceDTO>>> GetInvoicesForCurrentCustomerAsync(string userId);


    }
}
