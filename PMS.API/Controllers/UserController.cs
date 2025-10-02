using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.UserService;
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
        public async Task<ActionResult<User>> RegisterNewUser([FromBody] RegisterUser customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Dữ liệu không hợp lệ");
            }

            try
            {
                await _userService.RegisterUserAsync(customer);
                return Ok(new { Phone = customer.PhoneNumber, Email = customer.Email, FullName = customer.FullName });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //https://localhost:7213/api/User/verify
        [HttpGet("verify")]
        public async Task<ActionResult> Verify([FromQuery] string token)
        {
            try
            {
                await _userService.VerifyJwtTokenAsync(token);
                return Ok("Xác thực tài khoản thành công");
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
            if (!ModelState.IsValid)
            {
                return BadRequest("Email không hợp lệ");
            }

            bool success = await _userService.InitiatePasswordResetAsync(request.Email);
            if (!success)
            {
                return BadRequest("Email không tồn tại");
            }

            return Ok("Link đặt lại mật khẩu đã được gửi đến email của bạn");
        }

        //https://localhost:7213/api/User/forgot-password
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Dữ liệu không hợp lệ");
            }

            try
            {
                await _userService.ResetPasswordAsync(request.Token, request.NewPassword);

                return Ok("Mật khẩu đã được đặt lại thành công");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
