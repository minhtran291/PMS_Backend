using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public sealed class FEFOPlanItemDTO
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}
