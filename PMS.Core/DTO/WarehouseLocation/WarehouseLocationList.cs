using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.WarehouseLocation
{
    public class WarehouseLocationList
    {
        public int Id { get; set; }
        //public int WarehouseId { get; set; }
        public int RowNo { get; set; }
        public int ColumnNo { get; set; }
        public int LevelNo { get; set; }
        public WarehouseLocationStatus Status { get; set; }
    }
}
