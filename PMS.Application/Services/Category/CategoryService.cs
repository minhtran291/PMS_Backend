using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PMS.Application.DTOs.Category;
using PMS.Application.DTOs.Product;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.Category
{
    public class CategoryService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration) : Service(unitOfWork, mapper), ICategoryService
    {
        public async Task<ServiceResult<bool>> ActiveSupplierAsync(int cateId)
        {
            try
            {

                var excate = await _unitOfWork.Category.Query()
                    .FirstOrDefaultAsync(c => c.CategoryID == cateId);

                if (excate == null)
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        Message = $"Không tìm thấy loại sản phẩm với ID: {cateId}",
                        StatusCode = 404
                    };
                }


                excate.Status = !excate.Status;


                _unitOfWork.Category.Update(excate);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    Data = true,
                    Message = $"Đã {(excate.Status ? "kích hoạt" : "vô hiệu hóa")} loại sản phẩm thành công.",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Message = $"Đã xảy ra lỗi: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ServiceResult<bool>> AddAsync(CategoryDTO category)
        {
            try
            {
                if (category == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "kiểm tra lại dữ liệu",
                        Data = false
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
                    Status = true,
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
                    Status = p.Status,
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
                var category = await _unitOfWork.Category.Query().Include(c => c.Products).FirstOrDefaultAsync(p => p.CategoryID == id);

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
                    CategoryID = category.CategoryID,
                    Name = category.Name,
                    Description = category.Description,
                    Status = category.Status,

                    Products = category.Products.Select(p => new ProductDTO
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        Unit = p.Unit,
                        CategoryID = p.CategoryID,
                        Image = p.Image,
                        MinQuantity = p.MinQuantity,
                        MaxQuantity = p.MaxQuantity,
                        TotalCurrentQuantity = p.TotalCurrentQuantity,
                        Status = p.Status
                    }).ToList()
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

        public async Task<ServiceResult<bool>> UpdateCategoryAsync(CategoryDTO category)
        {

            try
            {
                var excate = await _unitOfWork.Category.Query().FirstOrDefaultAsync(e => e.CategoryID == category.CategoryID);
                if (excate == null)
                {
                    return new ServiceResult<bool>()
                    {
                        Data = false,
                        Message = $"Không tìm thấy categoryID:{category.CategoryID}",
                        StatusCode = 200,
                    };
                }

                excate.Name = category.Name;
                excate.Description = category.Description;
                excate.Status = category.Status;
                _unitOfWork.Category.Update(excate);
                await _unitOfWork.CommitAsync();
                return new ServiceResult<bool>()
                {
                    Data = true,
                    Message = $"CategoryID={category.CategoryID} đã update thành công",
                    StatusCode = 200,
                };
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Lỗi khi lấy thông tin thể loại sản phẩm: {ex.Message}", ex);
            }
        }
    }
}
