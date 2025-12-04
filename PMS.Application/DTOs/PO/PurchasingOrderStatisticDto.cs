using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class PurchasingOrderStatisticDto
    {
        public int Year { get; set; }
        public List<MonthlyOrderProductDto> MonthlyData { get; set; } = new();
    }
}
