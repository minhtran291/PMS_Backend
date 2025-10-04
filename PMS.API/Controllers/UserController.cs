using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PMS.API.Services.User;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Content;
using PMS.Core.DTO.Request;
using PMS.Data.UnitOfWork;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
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
            try
            {
                await _userService.RegisterUserAsync(customer);
                return Ok("Đăng ký tài khoản thành công");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //https://localhost:7213/api/User/confirm-email
        [HttpGet("confirm-email")]
        public async Task<ActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            try
            {
                await _userService.ConfirmEmailAsync(userId, token);
                return Ok("Xác thực tài khoản thành công");
            }
            catch (SecurityTokenExpiredException)
            {
                return Unauthorized(new { error = "Token đã hết hạn" });
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return Unauthorized(new { error = "Chữ ký token không hợp lệ" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //https://localhost:7213/api/User/forgot-password
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _userService.SendEmailResetPasswordAsync(request.Email);
                return Ok("Link đặt lại mật khẩu đã được gửi đến email của bạn");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //https://localhost:7213/api/User/reset-password
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _userService.ResetPasswordAsync(request);
                return Ok("Mật khẩu đã được đặt lại thành công");
            }
            catch (SecurityTokenExpiredException)
            {
                return Unauthorized(new { error = "Token đã hết hạn" });
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return Unauthorized(new { error = "Chữ ký token không hợp lệ" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("resend-confirm-email")]
        public async Task<IActionResult> ResendConfirmEmail(ResendConfirmEmailRequest email)
        {
            try
            {
                await _userService.ReSendEmailConfirmAsync(email);
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
