using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Auth;
using PMS.Application.DTOs.Auth;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;

        public LoginController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tokenResponse = await _loginService.Login(request);

            if(tokenResponse.Data != null)
            {
                Response.Cookies.Append("X-Refresh-Token", tokenResponse.Data.RefreshToken, new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.Now.AddDays(10)
                });
            }

            return StatusCode(tokenResponse.StatusCode, new
            {
                message = tokenResponse.Message,
                data = tokenResponse.Data?.AccessToken,
            });

        }
    }
}
