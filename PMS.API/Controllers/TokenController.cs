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
        public async Task<ActionResult<TokenResponse>> RefreshAccessToken(TokenRequest tokenRequest)
        {
            try
            {
                string? refreshToken = Request.Cookies["X-Refresh-Token"];

                if (string.IsNullOrEmpty(refreshToken))
                    return Unauthorized("Missing refresh token");

                var tokenResponse = await _jwtService.RefreshAccessToken(tokenRequest, refreshToken);

                Response.Cookies.Append("X-Refresh-Token", tokenResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(10)
                });

                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost, Authorize]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Invalid user id in token");

                await _jwtService.Revoke(userId);

                Response.Cookies.Delete("X-Refresh-Token");

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
