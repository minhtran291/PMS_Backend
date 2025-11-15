using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GRN
{
    public class PODetailViewDTO
    {
        public int PODID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string DVT { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPriceTotal { get; set; }
        public string Description { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public decimal RemainingQty { get; set; } 
    }
}
