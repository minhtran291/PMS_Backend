using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.ProductService;
using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly  IProductService _productService;
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
        public async Task<IActionResult> SetProductStatus(int productId, [FromBody] bool status)
        {
            try
            {
                if (productId < 0)
                {
                    return BadRequest("ID sản phẩm không hợp lệ");
                }

                await _productService.SetProductStatusAsync(productId, status);
                return Ok($"Sản phẩm đã được {(status ? "kích hoạt" : "vô hiệu hóa")} thành công");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật trạng thái sản phẩm");
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Product/getbyid/{productId}
        /// endpoint trả về sản phẩm với id cụ thể
        /// </summary>
        /// <param name="productId"></param>
        /// <returns><Product></returns>
        [HttpGet("getbyid/{productId}")]
        public async Task<ActionResult<ProductUpdate>> GetProductById(int productId)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                return Ok(product);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Product/active
        /// endpoint trả về list các sản phẩm với status là true
        /// </summary>
        /// <returns>List<ProductUpdate></returns>
        [HttpGet("active")]
        public async Task<ActionResult<List<ProductUpdate>>> GetActiveProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsWithStatusAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Product/all
        /// endpoint trả về list các sản phẩm
        /// </summary>
        /// <returns>IEnumerable<ProductUpdate></returns>
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<ProductUpdate>>> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);                                 
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Product/create
        /// endpoint tạo mới một sản phẩm
        /// </summary>
        /// <param name="product"></param>
        /// <returns><Product></returns>
        [HttpPost("create")]
        public async Task<ActionResult<Product>> AddProduct([FromBody] ProductUpdate product)
        {
            try
            {
                await _productService.AddProductAsync(product);
                return Ok(new { message = "Product created successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Product/update/{productId}
        /// endpoint update sản phẩm với id cụ thể
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="productUpdate"></param>
        /// <returns>void</returns>
        [HttpPut("update/{productId}")]
        public async Task<ActionResult> UpdateProduct(int productId, [FromBody] ProductUpdateDTO productUpdate)
        {
            try
            {
                await _productService.UpdateProductAsync(productId, productUpdate);
                return Ok(new { message = "Product updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
