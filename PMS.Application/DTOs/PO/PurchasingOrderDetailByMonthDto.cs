using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PO
{
    public class PurchasingOrderDetailByMonthDto
    {
        public int POID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
        public decimal Deposit { get; set; }
        public decimal Debt { get; set; }
        public PurchasingOrderStatus Status { get; set; }
        public int QID { get; set; }
        public string CreatedBy { get; set; }

        public List<PurchasingOrderDetailItemDto> Details { get; set; } = new();
    }
}
