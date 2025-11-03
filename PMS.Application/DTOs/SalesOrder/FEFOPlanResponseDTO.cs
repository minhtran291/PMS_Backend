using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public sealed class FEFOPlanResponseDTO
    {
        public int QID { get; set; }
        public List<FEFOProductPlanDTO> Products { get; set; } = new();
    }
}
