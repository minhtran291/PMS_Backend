using PMS.Core.Domain.Enums;
using PMS.Application.DTOs.WarehouseLocation;

namespace PMS.Application.DTOs.Warehouse
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
