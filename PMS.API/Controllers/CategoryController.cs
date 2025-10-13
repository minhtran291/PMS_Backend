using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Category;
using PMS.Core.Domain.Constant;
using PMS.Application.DTOs.Category;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// https://localhost:7213/api/Category/create
        /// Tạo thể loại mới
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDTO category)
        {
            var result = await _categoryService.AddAsync(category);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// https://localhost:7213/api/Category/all
        /// Lấy tất cả thể loại
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllAsync();
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// https://localhost:7213/api/Category/getbyid/{id}
        /// Lấy thể loại theo ID
        /// </summary>
        [HttpGet("getbyid/{id}")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var result = await _categoryService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
    }
}
