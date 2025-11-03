using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class WarehouseLocationDetailsDTO : WarehouseLocationDTO
    {
        public List<LotProductDTO> LotProduct { get; set; } = [];
    }
}
