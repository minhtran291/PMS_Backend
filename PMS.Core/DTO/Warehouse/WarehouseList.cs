using PMS.Core.Domain.Enums;
using PMS.Core.DTO.WarehouseLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Warehouse
{
    public class WarehouseList
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public WarehouseStatus Status { get; set; }
        public List<WarehouseLocationList> WarehouseLocationLists { get; set; }
    }
}
