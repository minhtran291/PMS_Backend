using PMS.Application.DTOs.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.SmartCA
{
    public interface ISmartCAService
    {
        Task<SmartCASignResult> SignPdfHashAsync(byte[] pdfBytes,string docId,
        SmartCASignInvoiceRequestDTO userInfo,
        CancellationToken cancellationToken = default);
    }
}
