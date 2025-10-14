using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Customer;
using PMS.Application.Services.User;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private readonly IUserService _userService;
        public ManagerController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy tất cả khách hàng có trạng thái Inactive (chưa kích hoạt).
        /// https://localhost:7213/api/Manager/inactive
        /// </summary>
        /// <returns>Danh sách khách hàng Inactive</returns>
        [HttpGet("inactive")]
        [ProducesResponseType(typeof(ServiceResult<IEnumerable<CustomerDTO>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllInactiveCustomers()
        {
            var result = await _userService.GetAllCustomerWithInactiveStatus();

            if (result.StatusCode == 200)
                return Ok(result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Duyệt và kích hoạt tài khoản khách hàng (chuyển trạng thái sang Active).
        /// https://localhost:7213/api/Manager/activate/{userId}
        /// </summary>
        /// <param name="userId">ID của khách hàng cần duyệt</param>
        /// <returns>Kết quả cập nhật trạng thái</returns>
        [HttpPut("activate/{userId}")]
        [ProducesResponseType(typeof(ServiceResult<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateCustomerStatus(string userId)
        {
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(managerId))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var result = await _userService.UpdateCustomerStatus(userId, managerId);

            if (result.Data)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
