using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class SmartCASignResult
    {
        public string TransactionId { get; set; } = default!;
        public string TranCode { get; set; } = default!;
    }
}
