using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class ProductPercentageDTO
    {
        public int LotId { get; set; }    
        public int Quantity { get; set; }       // Tổng số lượng Lot này trong tháng
        public decimal Percentage { get; set; } // % trên tổng số lượng tháng

        public ProductInfoDTO Product { get; set; } = new();
    }
}
