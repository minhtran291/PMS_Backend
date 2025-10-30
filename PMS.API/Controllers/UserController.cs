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
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {

        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        public UserController(IUserService userService, IWebHostEnvironment env)
        {
            _userService = userService;
            _env = env;
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
                400 => BadRequest(new
                {
                    success = false,
                    message = result.Message
                }),
                500 => StatusCode(500, new
                {
                    success = false,
                    message = result.Message
                }),

                _ => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                })
            };
        }

        //https://localhost:7213/api/User/resend-confirm-email
        [HttpPost("resend-confirm-email")]
        public async Task<IActionResult> ResendConfirmEmail([FromBody] ResendConfirmEmailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.ReSendEmailConfirmAsync(request);

            return result.StatusCode switch
            {
                200 => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                }),
                400 => BadRequest(new
                {
                    success = false,
                    message = result.Message
                }),
                500 => StatusCode(500, new
                {
                    success = false,
                    message = result.Message
                }),

                _ => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                })
            };
        }

        //https://localhost:7213/api/User/confirm-email
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest(new { Message = "UserId và Token không được để trống." });

            var result = await _userService.ConfirmEmailAsync(userId, token);

            return result.StatusCode switch
            {
                200 => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                }),
                400 => BadRequest(new
                {
                    success = false,
                    message = result.Message
                }),
                500 => StatusCode(500, new
                {
                    success = false,
                    message = result.Message
                }),

                _ => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                })
            };
        }

        //https://localhost:7213/api/User/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.SendEmailResetPasswordAsync(request.Email);

            return result.StatusCode switch
            {
                200 => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                }),
                400 => BadRequest(new
                {
                    success = false,
                    message = result.Message
                }),
                500 => StatusCode(500, new
                {
                    success = false,
                    message = result.Message
                }),

                _ => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                })
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
                200 => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                }),
                400 => BadRequest(new
                {
                    success = false,
                    message = result.Message
                }),
                500 => StatusCode(500, new
                {
                    success = false,
                    message = result.Message
                }),

                _ => Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                })
            };
        }

        /// <summary>
        /// https://localhost:7213/api/User/upload-avatar
        /// Upload avatar cho user
        /// </summary>
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
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

            var fileName = $"avatar_{Guid.NewGuid():N}{ext}";
            var physicalPath = Path.Combine(imagesDir, fileName);
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/images/{fileName}";
            var fullUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";
            return StatusCode(200, new { success = true, message = "Tải ảnh thành công", data = relativePath, fullUrl });
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

        /// <summary>
        /// https://localhost:7213/api/User/submit-additional-info
        /// Customer submit thông tin bổ sung (mã số thuế, mã số kinh doanh)
        /// </summary>
        [HttpPost("submit-additional-info")]
        public async Task<IActionResult> SubmitCustomerAdditionalInfo([FromBody] CustomerAdditionalInfoDTO additionalInfo)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _userService.SubmitCustomerAdditionalInfoAsync(userId, additionalInfo);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/User/customer-status
        /// Kiểm tra trạng thái customer và thông tin bổ sung
        /// </summary>
        [HttpGet("customer-status")]
        public async Task<IActionResult> GetCustomerStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _userService.GetCustomerStatusAsync(userId);
            return HandleServiceResult(result);
        }
    }
}