using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PO
{
    public class ViewDebtPODTO
    {
        public int poid { get; set; }
        public decimal toatlPo { get; set; }
        public decimal debt { get; set; }
        public PurchasingOrderStatus Status { get; set; }
    }
}
