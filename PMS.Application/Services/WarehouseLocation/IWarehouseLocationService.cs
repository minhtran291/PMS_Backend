using PMS.Application.DTOs.WarehouseLocation;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.WarehouseLocation
{
    public interface IWarehouseLocationService
    {
        Task<ServiceResult<object>> CreateWarehouseLocation(CreateWarehouseLocationDTO dto);
        Task <ServiceResult<object>>UpdateWarehouseLocation(UpdateWarehouseLocationDTO dto);
        Task<ServiceResult<object>> ViewWarehouseLocationDetails(int warehouseLocationId);
        Task<ServiceResult<List<WarehouseLocationDTO>>> GetListWarehouseLocation();
        Task<ServiceResult<object>> GetListByWarehouseId(int warehouseId);
        //Task<ServiceResult<bool>> StoringLotInWarehouseLocation(StoringLot dto);
        Task<ServiceResult<object>> DeleteWarehouseLocation(int warehouseLocationId);
    }
}
