using PMS.Application.DTOs.WarehouseLocation;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.WarehouseLocation
{
    public interface IWarehouseLocationService
    {
        Task CreateWarehouseLocation(CreateWarehouseLocation dto);
        Task UpdateWarehouseLocation(UpdateWarehouseLocation dto);
        Task<WarehouseLocationList> ViewWarehouseLocationDetails(int warehouseLocationId);
        Task<List<WarehouseLocationList>> GetListWarehouseLocation();
        Task<List<WarehouseLocationList>> GetListByWarehouseId(int warehouseId);
        //Task<ServiceResult<bool>> StoringLotInWarehouseLocation(StoringLot dto);
    }
}
