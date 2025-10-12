using PMS.Core.DTO.Supplier;

namespace PMS.API.Services.Supplier
{
    public interface ISupplierService
    {
        Task<SupplierResponseDTO> CreateAsync(CreateSupplierRequestDTO dto);
        Task<SupplierResponseDTO?> GetByIdAsync(int id);
        Task<IReadOnlyList<SupplierResponseDTO>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<SupplierResponseDTO> UpdateAsync(int id, UpdateSupplierRequestDTO dto);
        Task EnableSupplier(string supplierId);
        Task DisableSupplier(string supplierId);

    }
}
