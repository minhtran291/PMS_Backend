using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public sealed class LotPickDTO
    {
        public FEFOLotDTO Lot { get; set; } = null!;
        public decimal PickQuantity { get; set; }
    }
}
