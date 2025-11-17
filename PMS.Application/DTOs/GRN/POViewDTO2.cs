using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.GRN
{
    public class POViewDTO2
    {
        public int POID { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public int QID { get; set; }
        public decimal Debt { get; set; }
        public decimal Deposit { get; set; }
        public string PaymentBy { get; set; }
        public string UserName { get; set; }
        public DateTime? PaymentDate { get; set; }
        public PurchasingOrderStatus Status { get; set; }
        public ICollection<PODetailViewDTO> Details { get; set; }
    }
}
