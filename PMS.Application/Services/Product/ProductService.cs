﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Application.DTOs.Product;
using PMS.Data.UnitOfWork;
using Microsoft.Extensions.Configuration;

namespace PMS.Application.Services.Product
{
    public class ProductService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration) : Service(unitOfWork, mapper), IProductService
    
    {
        public async Task<ServiceResult<bool>> AddProductAsync(ProductDTO product)
        {
            try
            {
                if (product == null)
                {

                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "Dữ liệu sản phẩm không được để trống",
                        Data = false,
                    };
                }

                if (product.MinQuantity > product.MaxQuantity)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Số lượng tối thiểu không được lớn hơn số lượng tối đa",
                        Data = false,
                    };
                }

                if (product.TotalCurrentQuantity < 0)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Đảm bảo là số nguyên dương",
                        Data = false,
                    };
                }

                var category = await _unitOfWork.Category.Query().FirstOrDefaultAsync(c => c.CategoryID == product.CategoryID);
                if (category == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Danh mục không tồn tại",
                        Data = false,
                    };
                }

                var existingProduct = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductName == product.ProductName);
                if (existingProduct != null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Tên trùng vui lòng chọn tên khác",
                        Data = false,
                    };
                }


                var newProduct = new PMS.Core.Domain.Entities.Product
                {
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    ProductDescription = product.ProductDescription,
                    CategoryID = product.CategoryID,
                    MinQuantity = product.MinQuantity,
                    MaxQuantity = product.MaxQuantity,
                    TotalCurrentQuantity = product.TotalCurrentQuantity,
                    Status = false,
                    Image = product.Image,

                };
                await _unitOfWork.Product.AddAsync(newProduct);
                await _unitOfWork.CommitAsync();
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Thêm mới sản phẩm thành công",
                    Data = true,
                };
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Lỗi khi thêm sản phẩm: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Đã xảy ra lỗi khi thêm sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<ProductDTO>>> GetAllProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Product.GetAllAsync();
                var productList = products.Select(p => new ProductDTO
                {
                    ProductName = p.ProductName,
                    ProductDescription = p.ProductDescription,
                    CategoryID = p.CategoryID,
                    MinQuantity = p.MinQuantity,
                    MaxQuantity = p.MaxQuantity,
                    TotalCurrentQuantity = p.TotalCurrentQuantity,
                    Status = p.Status,
                    Unit = p.Unit,
                    Image = p.Image

                }).ToList();

                if (!productList.Any())
                {
                    return new ServiceResult<IEnumerable<ProductDTO>>
                    {
                        StatusCode = 200,
                        Message = "Hiện trong kho đang chưa có sản phẩm nào",
                        Data = null,
                    };
                }
                return new ServiceResult<IEnumerable<ProductDTO>>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = productList,
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<List<ProductDTO>>> GetAllProductsWithStatusAsync()
        {
            try
            {
                var products = _unitOfWork.Product.Query()
                    .Where(p => p.Status == true)
                    .Select(p => new ProductDTO
                    {
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        CategoryID = p.CategoryID,
                        MinQuantity = p.MinQuantity,
                        MaxQuantity = p.MaxQuantity,
                        TotalCurrentQuantity = p.TotalCurrentQuantity,
                        Status = p.Status,
                        Unit = p.Unit,
                        Image = p.Image
                    });

                var productList = await products.ToListAsync();

                if (!productList.Any())
                {
                    return new ServiceResult<List<ProductDTO>>
                    {
                        StatusCode = 200,
                        Message = "Hiện không có sản phẩm nào có trạng thái hoạt động (Status = true)",
                        Data = null,
                    };

                }
                return new ServiceResult<List<ProductDTO>>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = productList,
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách sản phẩm có trạng thái hoạt động: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<ProductDTO?>> GetProductByIdAsync(int id)
        {
            try
            {
                if (id < 0)
                {
                    return new ServiceResult<ProductDTO?>
                    {
                        StatusCode = 200,
                        Message = "ID sản phẩm không hợp lệ",
                        Data = null,
                    };
                }

                var product = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductID == id);
                if (product == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy sản phẩm với ID cung cấp");
                }

                var productUpdate = new ProductDTO
                {
                    ProductName = product.ProductName,
                    ProductDescription = product.ProductDescription,
                    Unit = product.Unit,
                    CategoryID = product.CategoryID,
                    MinQuantity = product.MinQuantity,
                    MaxQuantity = product.MaxQuantity,
                    TotalCurrentQuantity = product.TotalCurrentQuantity,
                    Status = product.Status,
                    Image = product.Image
                };

                return new ServiceResult<ProductDTO?>
                {
                    StatusCode = 200,
                    Message = "ID sản phẩm không hợp lệ",
                    Data = productUpdate,
                };
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Lỗi khi lấy thông tin sản phẩm: {ex.Message}", ex);

            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException($"Lỗi khi lấy thông tin sản phẩm: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Đã xảy ra lỗi khi lấy thông tin sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<bool>> SetProductStatusAsync(int productId, bool status)
        {
            try
            {

                if (productId <= 0)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "ID sản phẩm không hợp lệ",
                        Data = false
                    };
                }


                var product = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductID == productId);
                if (product == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Không tìm thấy sản phẩm với ID cung cấp",
                        Data = false
                    };
                }


                product.Status = status;

                _unitOfWork.Product.Update(product);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = true
                };
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Lỗi khi cập nhật trạng thái sản phẩm: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Đã xảy ra lỗi khi cập nhật trạng thái sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task <ServiceResult<bool>> UpdateProductAsync(int id, ProductUpdateDTO productUpdate)
        {
            try
            {
                if (id < 0)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "ID không hợp lệ",
                        Data = false,
                    };
                }

                if (productUpdate == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "dữ liệu không được phép để trống",
                        Data = false,
                    };
                }

                var exProduct = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductID == id);

                if (exProduct == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Không tìm thấy sản phẩm với ID cung cấp",
                        Data = false,
                    };
                }

                if (productUpdate.MinQuantity > productUpdate.MaxQuantity)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Số lượng tối thiểu không được lớn hơn số lượng tối đa",
                        Data = false,
                    };
                }

                if (productUpdate.TotalCurrentQuantity < 0)
                {
                    return new ServiceResult<bool> { StatusCode = 200, Message = "Đảm bảo là số nguyên dương", Data = false };

                }

                var category = await _unitOfWork.Category.Query().FirstOrDefaultAsync(c => c.CategoryID == productUpdate.CategoryID);
                if (category == null)
                {
                    return new ServiceResult<bool> { StatusCode = 200, Message = "Danh mục không tồn tại", Data = false };
                }

                exProduct.ProductName = productUpdate.ProductName ?? exProduct.ProductName;
                exProduct.Unit = productUpdate.Unit ?? exProduct.Unit;
                exProduct.CategoryID = productUpdate.CategoryID;
                exProduct.Image = productUpdate.Image ?? exProduct.Image;
                exProduct.MinQuantity = productUpdate.MinQuantity;
                exProduct.MaxQuantity = productUpdate.MaxQuantity;
                exProduct.TotalCurrentQuantity = productUpdate.TotalCurrentQuantity;
                exProduct.Status = productUpdate.Status;
                exProduct.ProductDescription = productUpdate.ProductDescription;


                _unitOfWork.Product.Update(exProduct);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = true,
                };

            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Lỗi khi cập nhật sản phẩm: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Đã xảy ra lỗi khi cập nhật sản phẩm: {ex.Message}", ex);

            }
        }


    }
}
