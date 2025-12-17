using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PMS.Application.DTOs.Product;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Data.UnitOfWork;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PMS.Application.Services.Product
{
    public class ProductService(IUnitOfWork unitOfWork, IMapper mapper, IWebHostEnvironment webEnv) : Service(unitOfWork, mapper), IProductService

    {


        public async Task<string> SaveAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File upload không hợp lệ");


            var rootPath = webEnv.WebRootPath;


            var folderPath = Path.Combine(rootPath, folder);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            return $"/{folder}/{fileName}".Replace("\\", "/");
        }

        public async Task<ServiceResult<bool>> AddProductAsync(ProductDTOView product)
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

                string? imageUrl = product.Image != null
            ? await SaveAsync(product.Image, "images/products/")
            : null;
                string? imageUrlA = product.ImageA != null
            ? await SaveAsync(product.ImageA, "images/products/")
            : null;
                string? imageUrlB = product.ImageB != null
            ? await SaveAsync(product.ImageB, "images/products/")
            : null;
                string? imageUrlC = product.ImageC != null
            ? await SaveAsync(product.ImageC, "images/products/")
            : null;
                string? imageUrlD = product.ImageD != null
            ? await SaveAsync(product.ImageD, "images/products/")
            : null;
                string? imageUrlE = product.ImageE != null
            ? await SaveAsync(product.ImageE, "images/products/")
            : null;
                var newProduct = new PMS.Core.Domain.Entities.Product
                {
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    ProductDescription = product.ProductDescription,
                    CategoryID = product.CategoryID,
                    MinQuantity = product.MinQuantity,
                    MaxQuantity = product.MaxQuantity,
                    TotalCurrentQuantity = 0,
                    Status = product.Status,
                    Image = imageUrl,
                    ImageA = imageUrlA,
                    ImageB = imageUrlB,
                    ImageC = imageUrlC,
                    ImageD = imageUrlD,
                    ImageE = imageUrlE,

                    ProductIngredients = product.ProductIngredients,
                    ProductWeight = product.ProductWeight,
                    ProductlUses = product.ProductlUses,
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
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    ProductDescription = p.ProductDescription,
                    CategoryID = p.CategoryID,
                    MinQuantity = p.MinQuantity,
                    MaxQuantity = p.MaxQuantity,
                    TotalCurrentQuantity = p.TotalCurrentQuantity,
                    Status = p.Status,
                    Unit = p.Unit,
                    Image = p.Image,
                    ImageA = p.ImageA,
                    ImageB = p.ImageB,
                    ImageC = p.ImageC,
                    ImageD = p.ImageD,
                    ImageE = p.ImageE,

                    ProductIngredients = p.ProductIngredients,
                    ProductWeight = p.ProductWeight,
                    ProductlUses = p.ProductlUses

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
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        CategoryID = p.CategoryID,
                        MinQuantity = p.MinQuantity,
                        MaxQuantity = p.MaxQuantity,
                        TotalCurrentQuantity = p.TotalCurrentQuantity,
                        Status = p.Status,
                        Unit = p.Unit,
                        Image = p.Image,
                        ImageA = p.ImageA,
                        ImageB = p.ImageB,
                        ImageC = p.ImageC,
                        ImageD = p.ImageD,
                        ImageE = p.ImageE,

                        ProductlUses = p.ProductlUses,
                        ProductWeight = p.ProductWeight,
                        ProductIngredients = p.ProductIngredients,
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
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ProductDescription = product.ProductDescription,
                    Unit = product.Unit,
                    CategoryID = product.CategoryID,
                    MinQuantity = product.MinQuantity,
                    MaxQuantity = product.MaxQuantity,
                    TotalCurrentQuantity = product.TotalCurrentQuantity,
                    Status = product.Status,
                    Image = product.Image,
                    ImageA = product.ImageA,
                    ImageB = product.ImageB,
                    ImageC = product.ImageC,
                    ImageD = product.ImageD,
                    ImageE = product.ImageE,

                    ProductIngredients = product.ProductIngredients,
                    ProductWeight = product.ProductWeight,
                    ProductlUses = product.ProductlUses,
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

        public async Task<ServiceResult<bool>> UpdateProductAsync(int id, ProductUpdateDTO2 productUpdate)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<bool>.Fail("ID không hợp lệ");

                if (productUpdate == null)
                    return ServiceResult<bool>.Fail("Dữ liệu không được để trống");

                var exProduct = await _unitOfWork.Product.Query()
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (exProduct == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy sản phẩm");

                
                if (productUpdate.MinQuantity.HasValue && productUpdate.MaxQuantity.HasValue &&
                    productUpdate.MinQuantity > productUpdate.MaxQuantity)
                {
                    return ServiceResult<bool>.Fail("Số lượng tối thiểu không được lớn hơn số lượng tối đa");
                }

                
                if (productUpdate.CategoryID.HasValue)
                {
                    var categoryExists = await _unitOfWork.Category.Query()
                        .AnyAsync(c => c.CategoryID == productUpdate.CategoryID.Value);

                    if (!categoryExists)
                        return ServiceResult<bool>.Fail("Danh mục không tồn tại");

                    exProduct.CategoryID = productUpdate.CategoryID.Value;
                }

                
                if (productUpdate.Image != null)
                    exProduct.Image = await SaveAsync(productUpdate.Image, "images/products/");

                if (productUpdate.ImageA != null)
                    exProduct.ImageA = await SaveAsync(productUpdate.ImageA, "images/products/");

                if (productUpdate.ImageB != null)
                    exProduct.ImageB = await SaveAsync(productUpdate.ImageB, "images/products/");

                if (productUpdate.ImageC != null)
                    exProduct.ImageC = await SaveAsync(productUpdate.ImageC, "images/products/");

                if (productUpdate.ImageD != null)
                    exProduct.ImageD = await SaveAsync(productUpdate.ImageD, "images/products/");

                if (productUpdate.ImageE != null)
                    exProduct.ImageE = await SaveAsync(productUpdate.ImageE, "images/products/");

                
                exProduct.ProductName = productUpdate.ProductName ?? exProduct.ProductName;
                exProduct.Unit = productUpdate.Unit ?? exProduct.Unit;
                exProduct.ProductDescription = productUpdate.ProductDescription ?? exProduct.ProductDescription;
                exProduct.ProductIngredients = productUpdate.ProductIngredients ?? exProduct.ProductIngredients;
                exProduct.ProductlUses = productUpdate.ProductlUses ?? exProduct.ProductlUses;
                exProduct.ProductWeight = productUpdate.ProductWeight ?? exProduct.ProductWeight;

                
                if (productUpdate.MinQuantity.HasValue)
                    exProduct.MinQuantity = productUpdate.MinQuantity.Value;

                if (productUpdate.MaxQuantity.HasValue)
                    exProduct.MaxQuantity = productUpdate.MaxQuantity.Value;

                if (productUpdate.Status.HasValue)
                    exProduct.Status = productUpdate.Status.Value;

                _unitOfWork.Product.Update(exProduct);
                await _unitOfWork.CommitAsync();

                return ServiceResult<bool>.SuccessResult(true, "Cập nhật sản phẩm thành công");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật sản phẩm: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<List<ProductDTO>>> SearchProductByKeyWordAsync(string keyWord)
        {
            var products = await _unitOfWork.Product
                .Query()
                .AsNoTracking()
                .Where(c => c.ProductName.Contains(keyWord))
                .Select(p => new ProductDTO
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    Unit = p.Unit,
                    CategoryID = p.CategoryID,
                    MaxQuantity = p.MaxQuantity,
                    MinQuantity = p.MinQuantity,
                    TotalCurrentQuantity = p.TotalCurrentQuantity,
                    Image = p.Image,
                    ImageA = p.ImageA,
                    ImageB = p.ImageB,
                    ImageC = p.ImageC,
                    ImageD = p.ImageD,
                    ImageE = p.ImageE,
                    ProductDescription = p.ProductDescription,
                    Status = p.Status,

                    ProductlUses = p.ProductlUses,
                    ProductWeight = p.ProductWeight,
                    ProductIngredients = p.ProductIngredients

                })
                .ToListAsync();

            if (!products.Any())
            {
                return new ServiceResult<List<ProductDTO>>
                {
                    Data = new List<ProductDTO>(),
                    Success = false,
                    Message = $"Không tồn tại sản phẩm nào với keyword '{keyWord}'",
                    StatusCode = 404
                };
            }

            return new ServiceResult<List<ProductDTO>>
            {
                Data = products,
                Success = true,
                Message = "Thành công",
                StatusCode = 200
            };
        }

        public async Task<ServiceResult<List<LotProductDTO2>>> GetLotProductByProductId(int productId)
        {
            try
            {
                var products = await _unitOfWork.LotProduct.Query()
                    .Where(p => p.ProductID == productId)
                    .Include(p => p.Product)
                    .ToListAsync();

                if (!products.Any())
                    return ServiceResult<List<LotProductDTO2>>.Fail("Không tìm thấy lô hàng nào cho sản phẩm này.");

                var result = products.Select(p => new LotProductDTO2
                {
                    LotID = p.LotID,
                    InputDate = p.InputDate.ToString("dd/MM/yyyy"),
                    SalePrice = p.SalePrice,
                    InputPrice = p.InputPrice,
                    ProductName = p.Product?.ProductName ?? "Unknown",
                    ExpiredDate = p.ExpiredDate.ToString("dd/MM/yyyy"),
                    LotQuantity = p.LotQuantity,
                    SupplierID = p.SupplierID,
                    ProductID = p.ProductID,
                    WarehouselocationID = p.WarehouselocationID,
                    LastCheckedDate = p.LastCheckedDate
                }).ToList();

                return ServiceResult<List<LotProductDTO2>>.SuccessResult(result, "Lấy danh sách lô hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<LotProductDTO2>>.Fail($"Lỗi khi lấy lô sản phẩm: {ex.Message}");
            }
        }

        public async Task<IEnumerable<ProductMinQuantityDto>> GetProductsBelowMinQuantityAsync()
        {
            return await _unitOfWork.Product.Query()
               .Where(p => p.TotalCurrentQuantity < (p.MinQuantity * 1.5))
               .Select(p => new ProductMinQuantityDto
               {
                   ProductID = p.ProductID,
                   ProductName = p.ProductName,
                   MinQuantity = p.MinQuantity,
                   TotalCurrentQuantity = p.TotalCurrentQuantity
               })
               .ToListAsync();
        }

        public async Task<IEnumerable<ProductNearestLotDto>> GetProductsWithNearestLotAsync()
        {
            var products = await _unitOfWork.Product.Query()
                .Include(p => p.LotProducts)
                    .ThenInclude(l => l.Supplier)
                .Include(p => p.LotProducts)
                    .ThenInclude(l => l.WarehouseLocation)
                .ToListAsync();

            var result = products
                .Select(product =>
                {
                    var nearestLot = product.LotProducts
                        .OrderBy(l => l.ExpiredDate)
                        .FirstOrDefault();

                    if (nearestLot == null)
                        return null;

                    return new ProductNearestLotDto
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        TotalCurrentQuantity = product.TotalCurrentQuantity,

                        LotID = nearestLot.LotID,
                        InputDate = nearestLot.InputDate,
                        ExpiredDate = nearestLot.ExpiredDate,
                        LotQuantity = nearestLot.LotQuantity,
                        InputPrice = nearestLot.InputPrice,
                        SalePrice = nearestLot.SalePrice,

                        SupplierID = nearestLot.SupplierID,
                        SupplierName = nearestLot.Supplier?.Name ?? "",

                        WarehouseLocationID = nearestLot.WarehouselocationID,
                        WarehouseLocationName = nearestLot.WarehouseLocation?.LocationName ?? ""
                    };
                })
                .Where(x => x != null)
                .ToList()!;

            return result;
        }


    }
}
