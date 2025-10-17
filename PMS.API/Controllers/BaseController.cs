using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        /// <summary>
        /// Chuẩn hóa phản hồi HTTP dựa vào StatusCode 
        /// </summary>
        protected IActionResult HandleServiceResult<T>(ServiceResult<T> result)
        {
            if (result == null)
                return StatusCode(500, new { Message = "Lỗi hệ thống: ServiceResult null." });

            return result.StatusCode switch
            {
                200 => Ok(result),
                201 => StatusCode(201, result),
                204 => NoContent(),
                400 => BadRequest(result),
                401 => Unauthorized(result),
                403 => Forbid(),
                404 => NotFound(result),
                409 => Conflict(result),
                422 => UnprocessableEntity(result),
                500 => StatusCode(500, result),
                _ => StatusCode(result.StatusCode, result)
            };
        }
    }
}
