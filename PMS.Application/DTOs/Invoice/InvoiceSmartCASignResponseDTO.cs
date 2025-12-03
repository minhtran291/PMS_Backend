using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class InvoiceSmartCASignResponseDTO
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = default!;
        public string TransactionId { get; set; } = default!;
        public string TranCode { get; set; } = default!;
    }
}
