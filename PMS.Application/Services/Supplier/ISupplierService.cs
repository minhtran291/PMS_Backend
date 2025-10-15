using PMS.Application.DTOs.Supplier;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.Supplier
{
    public interface ISupplierService
    {
        Task<ServiceResult<SupplierResponseDTO>> CreateAsync(CreateSupplierRequestDTO dto);
        Task<ServiceResult<SupplierResponseDTO?>> GetByIdAsync(int id);
        Task<ServiceResult<List<SupplierResponseDTO>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<ServiceResult<SupplierResponseDTO>> UpdateAsync(int id, UpdateSupplierRequestDTO dto);
        Task<ServiceResult<bool>> EnableSupplier(string supplierId);
        Task<ServiceResult<bool>> DisableSupplier(string supplierId);
    }
}
