using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PO
{
    public class POQuantityStatus
    {
        public int POID { get; set; }
        public PurchasingOrderStatus Status { get; set; }
    }
}
