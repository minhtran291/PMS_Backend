
namespace PMS.Application.DTOs.WarehouseLocation
{
    public class WarehouseLocationList
    {
        public int Id { get; set; }
        public required string LocationName { get; set; }
        public bool Status { get; set; }
    }
}
