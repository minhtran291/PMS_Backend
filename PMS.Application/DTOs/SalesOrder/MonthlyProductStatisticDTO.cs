using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class MonthlyProductStatisticDTO
    {
        public int Month { get; set; }  
        public int TotalQuantity { get; set; }
        public List<ProductPercentageDTO> Products { get; set; } = new();

    }
}
