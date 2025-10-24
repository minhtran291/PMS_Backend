using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;

namespace PMS.Application.DTOs.PO
{
    public class POViewDTO
    {
        public int POID { get; set; }
        public decimal Total { get; set; }
        public required DateTime OrderDate
        { get; set; }
        public bool Status { get; set; } = false;
        public decimal Deposit { get; set; }
        public decimal Debt { get; set; }
        public DateTime PaymentDate { get; set; }

        public string UserName { get; set; } = string.Empty;
        public required int QID { get; set; }
        public string? PaymentBy { get; set; }

        public virtual ICollection<PurchasingOrderDetail>? Details { get; set; } = new List<PurchasingOrderDetail>();
    }
}
