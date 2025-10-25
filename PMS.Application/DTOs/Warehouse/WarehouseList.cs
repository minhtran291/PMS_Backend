using PMS.Core.Domain.Enums;
using PMS.Application.DTOs.WarehouseLocation;

namespace PMS.Application.DTOs.Warehouse
{
    public class WarehouseList
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public bool Status { get; set; }
        public List<WarehouseLocationList> WarehouseLocationLists { get; set; } = [];
    }
}
