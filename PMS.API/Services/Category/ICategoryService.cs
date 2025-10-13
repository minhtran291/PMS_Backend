using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;

namespace PMS.API.Services.CategoryService
{
    public interface ICategoryService
    {
        Task<ServiceResult<CategoryDTO>> GetByIdAsync(int id);
        Task<ServiceResult<IEnumerable<CategoryDTO>>> GetAllAsync();
        Task <ServiceResult<bool>> AddAsync(CategoryDTO category);
    }
}
