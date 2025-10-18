using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.DTOs.Auth;
using PMS.Application.DTOs.Customer;
using PMS.Application.Services.User;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Identity;
using PMS.Data.UnitOfWork;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {

        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        //https://localhost:7213/api/User/register
        [HttpPost("register")]
        public async Task<IActionResult> RegisterNewUser([FromBody] RegisterUser customer)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.RegisterUserAsync(customer);


            return result.StatusCode switch
            {
                200 => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                }),
                500 => StatusCode(500, new
                {
                    success = false,
                    message = result.Message
                }),

                _ => Ok(new
                {
                    success = false,
                    message = result.Message
                }),
            };
        }

        //https://localhost:7213/api/User/confirm-email
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var result = await _userService.ConfirmEmailAsync(userId, token);

            return result.StatusCode switch
            {
                200 => Ok(new { success = true, message = result.Message, data = result.Data }),
                400 => BadRequest(new { success = false, message = result.Message }),
                404 => NotFound(new { success = false, message = result.Message }),
                _ => BadRequest(new { success = false, message = result.Message })
            };
        }

        //https://localhost:7213/api/User/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.SendEmailResetPasswordAsync(request.Email);

            // Map StatusCode sang HTTP status code phù hợp
            return result.StatusCode switch
            {
                200 => Ok(new { success = true, message = result.Message, data = result.Data }),
                404 => NotFound(new { success = false, message = result.Message }),
                _ => BadRequest(new { success = false, message = result.Message }),
            };
        }

        //https://localhost:7213/api/User/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.ResetPasswordAsync(request);

            return result.StatusCode switch
            {
                200 => Ok(new { success = true, message = result.Message, data = result.Data }),
                404 => NotFound(new { success = false, message = result.Message }),
                500 => BadRequest(new { success = false, message = result.Message }),
                _ => BadRequest(new { success = false, message = result.Message })
            };
        }

        //https://localhost:7213/api/User/resend-confirm-email

        [HttpPost("resend-confirm-email")]
        public async Task<IActionResult> ResendConfirmEmail([FromBody] ResendConfirmEmailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.ReSendEmailConfirmAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ khách hàng.
        /// https://localhost:7213/api/User/CustomerProfileUpdate
        /// </summary>
        /// <param name="request">Thông tin hồ sơ khách hàng cần cập nhật</param>
        [HttpPut("CustomerProfileUpdate")]
        public async Task<IActionResult> UpdateCustomerProfile([FromBody] CustomerProfileDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .Select(e => new
                    {
                        Field = e.Key,
                        Errors = e.Value!.Errors.Select(er => er.ErrorMessage).ToArray()
                    });

                return BadRequest(new
                {
                    Message = "Dữ liệu không hợp lệ.",
                    Errors = errors
                });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            var result = await _userService.UpdateCustomerProfile(userId, request);
            if (result.Data)
                return Ok(result);
            else
                return BadRequest(result);
        }


        /// <summary>
        /// Lấy thông tin khách hàng theo userId
        /// </summary>
        /// <param name="userId">Id của người dùng</param>
        /// <returns>Thông tin khách hàng</returns>
        /// <remarks>GET: https://localhost:7213/api/User/viewprofile</remarks>
        [HttpGet("viewprofile")]
        public async Task<IActionResult> GetCustomerById()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            var result = await _userService.GetCustomerByIdAsync(userId);

            if (result.StatusCode == 404)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// https://localhost:7213/api/User/changePassword
        /// Đổi mật khẩu người dùng (yêu cầu nhập mật khẩu cũ)
        /// </summary>
        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            if (model == null)
                return BadRequest(new { Message = "Dữ liệu yêu cầu không hợp lệ." });
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _userService.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);
            return HandleServiceResult(result);
        }
    }

}

