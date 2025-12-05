using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class MonthlyRevenueDTO
    {
        public int Month { get; set; } 
        public decimal Amount { get; set; }     // Số tiền thu được trong tháng
        public decimal Percentage { get; set; } // % so với tổng năm
    }
}
