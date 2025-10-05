using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.CategoryService;
using PMS.Core.DTO.Content;

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
        /// Creates a new category
        /// </summary>
        /// <param name="category">Category data transfer object</param>
        /// <returns>Created category details</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDTO category)
        {
            try
            {
                await _categoryService.AddAsync(category);
                return Ok(new { Name=category.Name, Description=category.Description });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi thêm thể loại" });
            }
        }


        /// <summary>
        /// https://localhost:7213/api/Category/all
        /// Retrieves all categories
        /// </summary>
        /// <returns>List of category data transfer objects</returns>
        /// 

        [HttpGet("all")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {               
                if (ex.Message.Contains("Hiện chưa có loại sản phẩm nào"))
                {
                    return NotFound(new { message = ex.Message });
                }
                return StatusCode(500, new { message = $"Lỗi khi lấy loại sản phẩm: {ex.Message}" });
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Category/getbyid/{id}
        /// Retrieves a category by ID
        /// </summary>
        /// <param name="id">The ID of the category</param>
        /// <returns>Category data transfer object</returns>
        [HttpGet("getbyid/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID thể loại sản phẩm không hợp lệ" });
                }

                var category = await _categoryService.GetByIdAsync(id);
                return Ok(category);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { message = $"Đã xảy ra lỗi khi lấy thông tin thể loại sản phẩm: {ex.Message}" });
            }
        }


    }
}
