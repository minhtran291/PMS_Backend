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
    }
}
