using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Core.Domain.Constant;

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
        Task<ServiceResult<List<LotProductDTO>>> UpdatePhysicalInventoryAsync(string userId, int whlcid, List<PhysicalInventoryUpdateDTO> updates);

        Task<ServiceResult<IEnumerable<LotProductDTO>>> ReportPhysicalInventoryByMonth(int month, int year);
        Task<byte[]> GeneratePhysicalInventoryReportExcelAsync(int month, int year, string userId);

    }
}
