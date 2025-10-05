using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.API.Services.Base;
using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.ProductService
{
    public class ProductService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration) : Service(unitOfWork, mapper), IProductService
    {
        public async Task AddProductAsync(ProductUpdate product)
        {
            try
            {
                if (product == null)
                {
                    throw new ArgumentNullException(nameof(product), "Dữ liệu sản phẩm không được để trống");
                }

                if (product.MinQuantity > product.MaxQuantity)
                {
                    throw new ArgumentException("Số lượng tối thiểu không được lớn hơn số lượng tối đa");
                }

                if (product.TotalCurrentQuantity < 0)
                {
                    throw new ArgumentException("Đảm bảo là số nguyên dương");
                }

                var category = await _unitOfWork.Category.Query().FirstOrDefaultAsync(c => c.CategoryID == product.CategoryID);
                if (category == null)
                {
                    throw new ArgumentException("Danh mục không tồn tại");
                }

                var existingProduct = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductName == product.ProductName);
                if (existingProduct != null)
                {
                    throw new ArgumentException("Tên sản phẩm đã tồn tại, vui lòng chọn tên khác");
                }


                var newProduct = new Product
                {
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    ProductDescription = product.ProductDescription,
                    CategoryID = product.CategoryID,
                    InputPrice = product.InputPrice,
                    MinQuantity = product.MinQuantity,
                    MaxQuantity = product.MaxQuantity,
                    TotalCurrentQuantity = product.TotalCurrentQuantity,
                    Status = false,
                };
                await _unitOfWork.Product.AddAsync(newProduct);
                await _unitOfWork.CommitAsync();
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

        public async Task<IEnumerable<ProductUpdate>> GetAllProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Product.GetAllAsync();
                var productList = products.Select(p => new ProductUpdate
                {                    
                    ProductName = p.ProductName,
                    ProductDescription = p.ProductDescription,
                    CategoryID = p.CategoryID,
                    InputPrice = p.InputPrice,
                    MinQuantity = p.MinQuantity,
                    MaxQuantity = p.MaxQuantity,
                    TotalCurrentQuantity = p.TotalCurrentQuantity,
                    Status = p.Status,
                    Unit = p.Unit
                }).ToList();

                if (!productList.Any())
                {
                    throw new Exception("Hiện trong kho đang chưa có sản phẩm nào");
                }

                return productList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task<List<ProductUpdate>> GetAllProductsWithStatusAsync()
        {
            try
            {
                var products = _unitOfWork.Product.Query()
                    .Where(p => p.Status == true)
                    .Select(p => new ProductUpdate
                    {
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        CategoryID = p.CategoryID,
                        InputPrice = p.InputPrice,
                        MinQuantity = p.MinQuantity,
                        MaxQuantity = p.MaxQuantity,
                        TotalCurrentQuantity = p.TotalCurrentQuantity,
                        Status = p.Status,
                        Unit = p.Unit
                    });

                var productList = await products.ToListAsync();

                if (!productList.Any())
                {
                    throw new Exception("Hiện không có sản phẩm nào có trạng thái hoạt động (Status = true)");
                }

                return productList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách sản phẩm có trạng thái hoạt động: {ex.Message}", ex);
            }
        }

        public async Task<ProductUpdate?> GetProductByIdAsync(int id)
        {
            try
            {
                if (id < 0)
                {
                    throw new ArgumentException("ID sản phẩm không hợp lệ");
                }

                var product = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductID == id);
                if (product == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy sản phẩm với ID cung cấp");
                }

                var productUpdate = new ProductUpdate
                {
                    ProductName = product.ProductName,
                    ProductDescription = product.ProductDescription,
                    Unit = product.Unit,
                    CategoryID = product.CategoryID,
                    InputPrice = product.InputPrice,
                    MinQuantity = product.MinQuantity,
                    MaxQuantity = product.MaxQuantity,
                    TotalCurrentQuantity = product.TotalCurrentQuantity,
                    Status = product.Status
                };

                return productUpdate;
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

        public async Task UpdateProductAsync(int id, ProductUpdateDTO productUpdate)
        {
            try
            {
                if (id < 0)
                {
                    throw new ArgumentException("ID sản phẩm không hợp lệ");
                }

                if (productUpdate == null)
                {
                    throw new ArgumentNullException(nameof(productUpdate), "Dữ liệu cập nhật sản phẩm không được để trống");
                }

                var exProduct = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductID == id);

                if (exProduct == null)
                {
                    throw new Exception("Không tìm thấy sản phẩm với ID cung cấp");
                }

                if (productUpdate.MinQuantity > productUpdate.MaxQuantity)
                {
                    throw new ArgumentException("Số lượng tối thiểu không được lớn hơn số lượng tối đa");
                }

                if (productUpdate.TotalCurrentQuantity < 0)
                {
                    throw new ArgumentException("Đảm bảo là số nguyên dương");
                }

                var category = await _unitOfWork.Category.Query().FirstOrDefaultAsync(c => c.CategoryID == productUpdate.CategoryID);
                if (category == null)
                {
                    throw new ArgumentException("Danh mục không tồn tại");
                }

                exProduct.ProductName = productUpdate.ProductName ?? exProduct.ProductName;
                exProduct.Unit = productUpdate.Unit ?? exProduct.Unit;
                exProduct.CategoryID = productUpdate.CategoryID;
                exProduct.InputPrice = productUpdate.InputPrice;
                exProduct.MinQuantity = productUpdate.MinQuantity;
                exProduct.MaxQuantity = productUpdate.MaxQuantity;
                exProduct.TotalCurrentQuantity = productUpdate.TotalCurrentQuantity;
                exProduct.Status = productUpdate.Status;

                if (productUpdate.ProductDescription != null)
                {
                    exProduct.ProductDescription = productUpdate.ProductDescription;
                }

                _unitOfWork.Product.Update(exProduct);
                await _unitOfWork.CommitAsync();


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


        public async Task SetProductStatusAsync(int productId, bool status)
        {
            try
            {

                if (productId <= 0)
                {
                    throw new ArgumentException("ID sản phẩm không hợp lệ");
                }


                var product = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductID == productId);
                if (product == null)
                {
                    throw new ArgumentException("Không tìm thấy sản phẩm với ID cung cấp");
                }


                product.Status = status;

                _unitOfWork.Product.Update(product);
                await _unitOfWork.CommitAsync();
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
    }
}
