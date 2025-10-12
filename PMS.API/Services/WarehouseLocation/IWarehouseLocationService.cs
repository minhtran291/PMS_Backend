using PMS.Core.DTO.WarehouseLocation;

namespace PMS.API.Services.WarehouseLocation
{
    public interface IWarehouseLocationService
    {
        Task CreateWarehouseLocation(CreateWarehouseLocation dto);
        Task UpdateWarehouseLocation(UpdateWarehouseLocation dto);
        Task<WarehouseLocationList> ViewWarehouseLocationDetails(int warehouseLocationId);
        Task<List<WarehouseLocationList>> GetListWarehouseLocation();
        Task<List<WarehouseLocationList>> GetListByWarehouseId(int warehouseId);
    }
}
