using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class InvoiceUpdateDTO
    {
        public List<int> PaymentRemainIds { get; set; } = new();
    }
}
