using PMS.Core.DTO.Warehouse;

namespace PMS.API.Services.Warehouse
{
    public interface IWarehouseService
    {
        Task CreateWarehouseAsync(CreateWarehouse dto);
        Task UpdateWarehouseAsync(UpdateWarehouse dto);
        Task<WarehouseList> ViewWarehouseDetailsAysnc(int warehouseId);
        Task<List<WarehouseList>> GetListWarehouseAsync();
    }
}
