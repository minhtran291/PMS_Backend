using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.Product;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;

namespace PMS.Application.Services.Warehouse
{
    public interface IWarehouseService
    {
        Task<ServiceResult<object>> CreateWarehouseAsync(CreateWarehouseDTO dto);
        Task<ServiceResult<object>> UpdateWarehouseAsync(UpdateWarehouseDTO dto);
        Task<ServiceResult<WarehouseDetailsDTO>> ViewWarehouseDetailsAysnc(int warehouseId);
        Task<ServiceResult<List<WarehouseDTO>>> GetListWarehouseAsync();
        Task<ServiceResult<object>> DeleteWarehouseAsync(int warehouseId);
        Task<ServiceResult<List<LotProductDTO>>> GetAllLotByWHLID(int whlcid);
        Task<ServiceResult<LotProductDTO>> UpdateSalePriceAsync(int whlcid, int lotid, decimal newSalePrice);

        Task<ServiceResult<int>> CreateInventorySessionAsync(string userId, int whlcid);

        Task<ServiceResult<bool>> UpdateInventoryBatchAsync(string userId, UpdateInventoryBatchDto input);

        Task<ServiceResult<IEnumerable<InventoryCompareDTO>>> GetInventoryComparisonAsync(int sessionId);

        Task<ServiceResult<int>> CompleteInventorySessionAsync(int sessionId, string userId);
        Task<ServiceResult<IEnumerable<InventoryHistoryDTO>>> GetHistoriesBySessionIdAsync(int sessionId);
        Task<ServiceResult<byte[]>> ExportInventorySessionToExcelAsync(string userId, int sessionId);

        Task<ServiceResult<List<InventorySessionDTO>>> GetAllInventorySessionsAsync();

        Task<ServiceResult<List<InventorySessionDTO>>> GetAllInventorySessionsByWarehouseLocationAsync(int warehouseLocationId);
    }
}
