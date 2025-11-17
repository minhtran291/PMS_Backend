using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class InvoicePDFResultDTO
    {
        public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}
