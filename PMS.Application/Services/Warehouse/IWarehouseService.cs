using PMS.Application.DTOs.Warehouse;

namespace PMS.Application.Services.Warehouse
{
    public interface IWarehouseService
    {
        Task CreateWarehouseAsync(CreateWarehouse dto);
        Task UpdateWarehouseAsync(UpdateWarehouse dto);
        Task<WarehouseList> ViewWarehouseDetailsAysnc(int warehouseId);
        Task<List<WarehouseDTO>> GetListWarehouseAsync();
    }
}
