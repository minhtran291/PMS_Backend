using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public sealed class FEFOPlanRequestDTO
    {
        public int QID { get; set; }
        public List<FEFOPlanItemDTO> Items { get; set; } = new();
    }
}
