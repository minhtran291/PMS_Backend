using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public sealed class CreateOrderResponseDTO
    {
        public string OrderId { get; set; } = null!;
        public int QID { get; set; }
        public decimal OrderTotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public FEFOPlanResponseDTO PlanUsed { get; set; } = null!;
    }
}
