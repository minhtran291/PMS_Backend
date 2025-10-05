using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;

namespace PMS.API.Services.CategoryService
{
    public interface ICategoryService
    {
        Task<CategoryDTO> GetByIdAsync(int id);
        Task<IEnumerable<CategoryDTO>> GetAllAsync();
        Task AddAsync(CategoryDTO category);
        Task UpdateAsync(Category category);
    }
}
