using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class GenerateInvoiceFromPaymentRemainsRequestDTO
    {
        public int SalesOrderId { get; set; }
        public List<int> PaymentRemainIds { get; set; } = new();
    }
}
