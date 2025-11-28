
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using PMS.API.Helpers.AttributeRoles;
using PMS.API.Helpers.PermisstionStaff;
using PMS.Application.DTOs.Admin;
using PMS.Application.Services.Admin;
using PMS.Application.Services.Notification;
using PMS.Application.Services.User;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase

    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public AdminController(IAdminService adminService, IUserService userService, IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _adminService = adminService;
            _userService = userService;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }
        [Authorize(Roles = UserRoles.ADMIN )]
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
        [Authorize(Roles = UserRoles.ADMIN + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = UserRoles.ADMIN + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = UserRoles.ADMIN + "," + UserRoles.PURCHASES_STAFF + "," + UserRoles.SALES_STAFF+ "," + UserRoles.WAREHOUSE_STAFF + "," + UserRoles.ACCOUNTANT)]
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
        [Authorize(Roles = UserRoles.ADMIN + "," + UserRoles.MANAGER)]

        public async Task<IActionResult> UpdateCustomerStatus(string userId)
        {
            var verifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(verifier))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var result = await _userService.UpdateCustomerStatus(userId, verifier);

            if (result.Data)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Cấp quyền cho nhân viên duyệt tk khách theo userId => em dùng tạm cái màn quản lý nhân viên mà fetch Id nhé
        /// http://localhost:5137/api/Admin/grant-permission/{userId}
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpPost("grant-permission/{userId}")]
        [Authorize(Roles = UserRoles.ADMIN)]
        public async Task<IActionResult> GrantPermission(string userId)
        {
            var verifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(verifier))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy user" });

            var result = await _unitOfWork.Users.UserManager.AddClaimAsync(
                user,
                new Claim("Permission", Permissions.CAN_APPROVE_CUSTOMER)
            );
            await _notificationService.SendNotificationToCustomerAsync(
                    senderId: verifier,
                    user.Id,
                    title: $"Yêu cầu nhân viên duyệt tài khoản",
                    message: $"Thông báo Admin đã mở quyền duyệt tài khoản cho khách",
                    type: NotificationType.System);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Đã cấp quyền duyệt tài khoản cho nhân viên" });
        }

        /// <summary>
        /// thu hồi quyền duyệt
        /// http://localhost:5137/api/Admin/revoke-permission/{userId}
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("revoke-permission/{userId}")]
        [Authorize(Roles = UserRoles.ADMIN)]
        public async Task<IActionResult> RevokePermission(string userId)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy user" });

            var claims = await _unitOfWork.Users.UserManager.GetClaimsAsync(user);
            var targetClaim = claims.FirstOrDefault(c =>
                c.Type == "Permission" && c.Value == Permissions.CAN_APPROVE_CUSTOMER);

            if (targetClaim == null)
                return BadRequest(new { message = "User chưa có quyền này" });

            var result = await _unitOfWork.Users.UserManager.RemoveClaimAsync(user, targetClaim);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Đã thu hồi quyền duyệt tài khoản" });
        }

        /// <summary>
        /// kiểm tra quyền nhân viên
        /// http://localhost:5137/api/Admin/user-claims/{userId}
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("user-claims/{userId}")]
        [Authorize(Roles = UserRoles.ADMIN)]
        public async Task<IActionResult> GetUserClaims(string userId)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy user" });

            var claims = await _unitOfWork.Users.UserManager.GetClaimsAsync(user);

            return Ok(claims.Select(c => new { c.Type, c.Value }));
        }

    }
}
