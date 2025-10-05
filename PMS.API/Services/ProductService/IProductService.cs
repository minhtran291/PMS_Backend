using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;

namespace PMS.API.Services.ProductService
{
    public interface IProductService
    {
        Task<IEnumerable<ProductUpdate>> GetAllProductsAsync();

        Task<ProductUpdate?> GetProductByIdAsync(int id);

        Task AddProductAsync(ProductUpdate product);

        Task UpdateProductAsync(int id, ProductUpdateDTO productUpdate);

        Task<List<ProductUpdate>> GetAllProductsWithStatusAsync();

        Task SetProductStatusAsync(int productId, bool status);

    }
}
