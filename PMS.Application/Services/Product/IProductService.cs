﻿using PMS.Core.Domain.Constant;
using PMS.Application.DTOs.Product;

namespace PMS.Application.Services.Product
{
    public interface IProductService
    {
        Task<ServiceResult<bool>> AddProductAsync(ProductDTO product);
        Task<ServiceResult<IEnumerable<ProductDTO>>> GetAllProductsAsync();
        Task<ServiceResult<List<ProductDTO>>> GetAllProductsWithStatusAsync();
        Task<ServiceResult<ProductDTO?>> GetProductByIdAsync(int id);
        Task<ServiceResult<bool>> UpdateProductAsync(int id, ProductUpdateDTO productUpdate);

        Task<ServiceResult<bool>> SetProductStatusAsync(int productId, bool status);
    }
}
