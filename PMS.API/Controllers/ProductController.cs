using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Product;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Application.DTOs.Product;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly  IProductService _productService;
        private readonly IWebHostEnvironment _env;
        public ProductController(IProductService productService, IWebHostEnvironment env)
        {
            _productService = productService;
            _env = env;
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
        public async Task<IActionResult> AddProduct([FromBody] ProductDTO product)
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
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> UpdateProduct(int productId, [FromBody] ProductUpdateDTO productUpdate)
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
        /// Upload product image and return relative path
        /// POST: /api/Product/upload-image
        /// </summary>
        [HttpPost("upload-image")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return StatusCode(400, new { success = false, message = "File không hợp lệ", data = (string?)null });
            }

            var allowedExt = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
            {
                return StatusCode(400, new { success = false, message = "Định dạng không hỗ trợ (chỉ jpg, jpeg, png)", data = (string?)null });
            }

            var imagesDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images");
            if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);

            var fileName = $"product_{Guid.NewGuid():N}{ext}";
            var physicalPath = Path.Combine(imagesDir, fileName);
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/images/{fileName}";
            var fullUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";
            return StatusCode(200, new { success = true, message = "Tải ảnh thành công", data = relativePath, fullUrl });
        }

    }
}
