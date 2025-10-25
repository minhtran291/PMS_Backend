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
    public class CategoryController : BaseController
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

        /// <summary>
        /// https://localhost:7213/api/Category/updatecategory
        /// Cập nhật thông tin danh mục sản phẩm
        /// </summary>
        [HttpPut("updatecategory")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> UpdateCategoryAsync([FromBody] CategoryDTO category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _categoryService.UpdateCategoryAsync(category);
                return HandleServiceResult(result); 
            }
            catch (ArgumentException ex)
            {
                return HandleServiceResult(new ServiceResult<bool>
                {
                    Data = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                return HandleServiceResult(new ServiceResult<bool>
                {
                    Data = false,
                    Message = $"Đã xảy ra lỗi hệ thống khi cập nhật danh mục: {ex.Message}",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Category/toggleStatus/{cateId}
        /// </summary>
        /// <param name="cateId"></param>
        /// <returns></returns>
        [HttpPut("toggleStatus/{cateId}")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> ToggleCategoryStatus(int cateId)
        {
            try
            {
                var result = await _categoryService.ActiveSupplierAsync(cateId);

                if (result.StatusCode == 404)
                    return NotFound(result);

                if (result.StatusCode == 500)
                    return StatusCode(500, result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Data = false,
                    Message = $"Đã xảy ra lỗi trong quá trình xử lý: {ex.Message}"
                });
            }
        }

    }
}
