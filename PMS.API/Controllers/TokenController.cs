using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Auth;
using PMS.Application.DTOs.Auth;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ITokenService _jwtService;

        public TokenController(ITokenService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<ActionResult<string>> RefreshAccessToken(TokenRequest tokenRequest)
        {
            string refreshToken = Request.Cookies["X-Refresh-Token"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Missing refresh token");

            var tokenResponse = await _jwtService.RefreshAccessToken(tokenRequest, refreshToken);

            if (tokenResponse.Data != null)
            {
                Response.Cookies.Append("X-Refresh-Token", tokenResponse.Data.RefreshToken, new CookieOptions
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
                data = tokenResponse.Data?.AccessToken
            });
        }

        [HttpPost, Authorize]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid user id in token");

            var result = await _jwtService.Revoke(userId);

            Response.Cookies.Delete("X-Refresh-Token");

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }
    }
}
