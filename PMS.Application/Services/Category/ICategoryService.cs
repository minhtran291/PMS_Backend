using PMS.Core.Domain.Constant;
using PMS.Application.DTOs.Category;

namespace PMS.Application.Services.Category
{
    public interface ICategoryService
    {
        Task<ServiceResult<CategoryDTO>> GetByIdAsync(int id);
        Task<ServiceResult<IEnumerable<CategoryDTO>>> GetAllAsync();
        Task <ServiceResult<bool>> AddAsync(CategoryDTO category);
    }
}
