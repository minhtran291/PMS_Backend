using PMS.Application.DTOs.WarehouseLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Warehouse
{
    public class WarehouseDetailsDTO : WarehouseDTO
    {
        public List<WarehouseLocationDTO> WarehouseLocations { get; set; } = [];
    }
}
