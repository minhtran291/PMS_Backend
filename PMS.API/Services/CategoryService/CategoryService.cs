using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.API.Services.Base;
using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.CategoryService
{
    public class CategoryService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration) : Service(unitOfWork, mapper), ICategoryService
    {
        public async Task AddAsync(CategoryDTO category)
        {
            try
            {
                if (category == null)
                {
                    throw new ArgumentNullException(nameof(category), "Kiểm tra lại dữ liệu");
                }

                var existingCategory = await _unitOfWork.Category.Query().FirstOrDefaultAsync(p => p.Name == category.Name);
                if (existingCategory != null)
                {
                    throw new ArgumentException("Tên thể loại đã tồn tại, vui lòng chọn tên khác");
                }

                var newcategory = new Category
                {
                    Name = category.Name,
                    Description = category.Description,
                };

                await _unitOfWork.Category.AddAsync(newcategory);
                await _unitOfWork.CommitAsync();
            }
            catch (ArgumentNullException ex)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                throw;
            }
            catch (Exception ex)
            {

                throw new Exception("Đã xảy ra lỗi khi thêm thể loại", ex);
            }
        }

        public async Task<IEnumerable<CategoryDTO>> GetAllAsync()
        {

            try
            {
                var category = await _unitOfWork.Category.GetAllAsync();
                var categoryList = category.Select(p => new CategoryDTO
                {
                    CategoryID = p.CategoryID,
                    Name = p.Name,
                    Description = p.Description,
                }).ToList();


                if (!categoryList.Any())
                {
                    throw new Exception("Hiện chưa có loại sản phẩm nào");
                }
                return categoryList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy loại sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task<CategoryDTO> GetByIdAsync(int id)
        {
            try
            {
                if (id < 0)
                {
                    throw new ArgumentException("ID thể loại sản phẩm không hợp lệ");
                }
                var category = await _unitOfWork.Category.Query().FirstOrDefaultAsync(p => p.CategoryID == id);

                if (category == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy thể loại sản phẩm với ID cung cấp");
                }

                var categorydto = new CategoryDTO
                {
                    CategoryID=category.CategoryID,
                    Name = category.Name,
                    Description = category.Description,
                };

                return categorydto;
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Lỗi khi lấy thông tin thể loại sản phẩm: {ex.Message}", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException($"Lỗi khi lấy thông tin thể loại sản phẩm: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Đã xảy ra lỗi khi lấy thông tin thể loại sản phẩm: {ex.Message}", ex);
            }
        }

        public Task UpdateAsync(Category category)
        {
            throw new NotImplementedException();
        }
    }


}
