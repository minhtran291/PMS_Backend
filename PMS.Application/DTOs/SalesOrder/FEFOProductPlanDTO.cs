using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public sealed class FEFOProductPlanDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal RequestedQuantity { get; set; }
        public List<LotPickDTO> Picks { get; set; } = new();
        public bool IsFulfilled => Picks.Sum(p => p.PickQuantity) >= RequestedQuantity;
    }
}
