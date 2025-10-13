using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.WarehouseLocation
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
