using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class MonthlyOrderProductDto
    {
        public int Month { get; set; }
        public List<OrderProductDetailDto> Products { get; set; } = new();
    }
}
