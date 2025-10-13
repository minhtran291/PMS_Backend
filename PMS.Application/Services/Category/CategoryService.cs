using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Application.DTOs.Category;
using PMS.Data.UnitOfWork;
using Microsoft.Extensions.Configuration;

namespace PMS.Application.Services.Category
{
    public class CategoryService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration) : Service(unitOfWork, mapper), ICategoryService
    {
        public async Task<ServiceResult<bool>> AddAsync(CategoryDTO category)
        {
            try
            {
                if (category == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode=500,
                        Message="kiểm tra lại dữ liệu",
                        Data=false
                    };
                }

                var existingCategory = await _unitOfWork.Category.Query().FirstOrDefaultAsync(p => p.Name == category.Name);
                if (existingCategory != null)
                {

                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Tên thể loại đã tồn tại, vui lòng chọn tên khác",
                        Data = false
                    };
                }

                var newcategory = new Core.Domain.Entities.Category
                {
                    Name = category.Name,
                    Description = category.Description,
                };

                await _unitOfWork.Category.AddAsync(newcategory);
                await _unitOfWork.CommitAsync();
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = true
                };
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

        public async Task<ServiceResult<IEnumerable<CategoryDTO>>> GetAllAsync()
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
                    return new ServiceResult<IEnumerable<CategoryDTO>>
                    {
                        StatusCode = 200,
                        Message = "Hiện chưa có loại sản phẩm nào",
                        Data = null
                    };
                }
                return new ServiceResult<IEnumerable<CategoryDTO>>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = categoryList
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy loại sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<CategoryDTO>> GetByIdAsync(int id)
        {
            try
            {
                if (id < 0)
                {
                    return new ServiceResult<CategoryDTO>()
                    {
                        StatusCode = 200,
                        Message = "ID thể loại sản phẩm không hợp lệ",
                        Data = null
                    };
                }
                var category = await _unitOfWork.Category.Query().FirstOrDefaultAsync(p => p.CategoryID == id);

                if (category == null)
                {
                    return new ServiceResult<CategoryDTO>()
                    {
                        StatusCode = 200,
                        Message = "Không tìm thấy thể loại sản phẩm với ID cung cấp",
                        Data = null
                    };
                }

                var categorydto = new CategoryDTO
                {
                    CategoryID=category.CategoryID,
                    Name = category.Name,
                    Description = category.Description,
                };

                return new ServiceResult<CategoryDTO>()
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = categorydto
                };
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

    }
}
