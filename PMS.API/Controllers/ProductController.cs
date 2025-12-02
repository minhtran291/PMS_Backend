using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Product;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Application.DTOs.Product;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// https://localhost:7213/api/Product/{productId}/Status
        /// endpoint chuyển đổi status sản phẩm
        /// </summary>
        /// <param name="productId">ID của sản phẩm</param>
        /// <param name="status">Trạng thái mới (true: kích hoạt, false: vô hiệu hóa)</param>
        /// <returns>Thông báo thành công hoặc lỗi</returns>
        [HttpPut("{productId}/status")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> SetProductStatus(int productId, [FromBody] bool status)
        {
            var result = await _productService.SetProductStatusAsync(productId, status);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// https://localhost:7213/api/Product/getbyid/{productId}
        /// endpoint trả về sản phẩm với id cụ thể
        /// </summary>
        /// <param name="productId"></param>
        /// <returns><Product></returns>
        [HttpGet("getbyid/{productId}")]
        public async Task<IActionResult> GetProductById(int productId)
        {
            var result = await _productService.GetProductByIdAsync(productId);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// https://localhost:7213/api/Product/active
        /// endpoint trả về list các sản phẩm với status là true
        /// </summary>
        /// <returns>List<ProductUpdate></returns>
        [HttpGet("active")]
        // [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> GetActiveProducts()
        {
            var result = await _productService.GetAllProductsWithStatusAsync();
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// https://localhost:7213/api/Product/all
        /// endpoint trả về list các sản phẩm
        /// </summary>
        /// <returns>IEnumerable<ProductUpdate></returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllProducts()
        {
            var result = await _productService.GetAllProductsAsync();
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// https://localhost:7213/api/Product/create
        /// endpoint tạo mới một sản phẩm
        /// </summary>
        /// <param name="product"></param>
        /// <returns><Product></returns>
        [HttpPost("create")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> AddProduct([FromForm] ProductDTOView product)
        {


            var result = await _productService.AddProductAsync(product);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// https://localhost:7213/api/Product/update/{productId}
        /// endpoint update sản phẩm với id cụ thể
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="productUpdate"></param>
        /// <returns>void</returns>
        [HttpPut("update/{productId}")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "" + UserRoles.SALES_STAFF)]
        public async Task<IActionResult> UpdateProduct(int productId, [FromForm] ProductUpdateDTO productUpdate)
        {
            var result = await _productService.UpdateProductAsync(productId, productUpdate);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Tìm sản phẩm theo từ khóa (keyword)
        /// https://localhost:7213/api/Product/search
        /// </summary>
        /// <param name="keyword">Tên hoặc phần tên sản phẩm</param>
        /// <returns>Thông tin sản phẩm nếu tồn tại</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchProductByKeyword([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Keyword không được để trống"
                });
            }

            var result = await _productService.SearchProductByKeyWordAsync(keyword);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }


        /// <summary>
        /// http://localhost:5137/api/Product/SearchLotProductByproductId/{poid}
        /// </summary>
        /// <param name="poid"></param>
        /// <returns></returns>
        [HttpGet("SearchLotProductByproductId/{poid}")]
        public async Task<IActionResult> SearchLotProductByproductId(int poid)
        {
            var result = await _productService.GetLotProductByProductId(poid);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// http://localhost:5137/api/Product/belowMinQuantity
        /// </summary>
        /// <returns></returns>
        [HttpGet("belowMinQuantity")]
        public async Task<IActionResult> GetProductsBelowMinQuantity()
        {
            var products = await _productService.GetProductsBelowMinQuantityAsync();

            return Ok(new
            {
                success = true,
                total = products.Count(),
                data = products
            });
        }

        /// <summary>
        /// http://localhost:5137/api/Product/NearestLot
        /// </summary>
        /// <returns></returns>
        [HttpGet("NearestLot")]
        public async Task<IActionResult> GetProductsWithNearestLot()
        {
            var data = await _productService.GetProductsWithNearestLotAsync();

            return Ok(new
            {
                total = data.Count(),
                data
            });
        }

    }
}

