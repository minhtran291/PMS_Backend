
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Admin;
using PMS.Application.Services.Admin;
using PMS.Application.Services.User;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase

    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;

        public AdminController(IAdminService adminService, IUserService userService)
        {
            _adminService = adminService;
            _userService = userService;
        }
        [Authorize(Roles = UserRoles.ADMIN)]
        [HttpPost("create-staff-account")]
        public async Task<IActionResult> CreateStaffAccountAsync([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminService.CreateAccountAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
        [Authorize(Roles = UserRoles.ADMIN)]
        [HttpGet("get-account-list")]
        public async Task<IActionResult> AccountListAsync(string? keyword)
        {
            try
            {
                var list = await _adminService.GetAccountListAsync(keyword);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        /// <summary>
        /// https://localhost:7213/api/Admin/get-account-details
        /// get ....
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("get-account-details")]
        [Authorize(Roles = UserRoles.ADMIN)]
        public async Task<IActionResult> AccountDetailAsync(string userId)
        {
            var result = await _adminService.GetAccountDetailAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
        [Authorize(Roles = UserRoles.ADMIN)]
        [HttpPut("update-staff-account")]
        public async Task<IActionResult> UpdateAccountAsync([FromBody] UpdateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminService.UpdateAccountAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
        [Authorize(Roles = UserRoles.ADMIN)]
        [HttpPost("suspend-account")]
        public async Task<IActionResult> SuspendAccountAsync(string userId)
        {
            var result = await _adminService.SuspendAccountAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
        [Authorize(Roles = UserRoles.ADMIN)]
        [HttpPost("active-account")]
        public async Task<IActionResult> ActiveAccountAsync(string userID)
        {
            var result = await _adminService.ActiveAccountAsync(userID);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// Duyệt và kích hoạt tài khoản khách hàng (chuyển trạng thái sang Active).
        /// http://localhost:5137/api/Admin/activate/{userId}
        /// </summary>
        /// <param name="userId">ID của khách hàng cần duyệt</param>
        /// <returns>Kết quả cập nhật trạng thái</returns>
        [HttpPut("activate/{userId}")]
        [ProducesResponseType(typeof(ServiceResult<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        [Authorize(Roles = UserRoles.ADMIN)]
        public async Task<IActionResult> UpdateCustomerStatus(string userId)
        {
            var Admin = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(Admin))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var result = await _userService.UpdateCustomerStatus(userId, Admin);

            if (result.Data)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
