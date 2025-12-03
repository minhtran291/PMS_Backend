using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class PurchasingOrderDetailItemDto
    {
        public int PODID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string DVT { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPriceTotal { get; set; }
        public decimal Tax { get; set; }
        public DateTime ExpiredDate { get; set; }
    }
}
