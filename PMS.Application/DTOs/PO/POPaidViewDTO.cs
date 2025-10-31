using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PO
{
    public class POPaidViewDTO
    {
        public required PurchasingOrderStatus Status { get; set; }
        public required decimal Debt { get; set; }
        public required DateTime PaymentDate { get; set; }
        public required string? PaymentBy { get; set; }
    }
}
