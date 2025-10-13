using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Services.Base;
using PMS.Application.Services.User;
using PMS.Core.ConfigOptions;
using PMS.Application.DTOs.Auth;
using PMS.Data.UnitOfWork;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PMS.Application.Services.Auth
{
    public class TokenService(IUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<JwtConfig> jwtConfig,
        IUserService userService,
        ILogger<TokenService> logger) : Service(unitOfWork, mapper), ITokenService
    {
        private readonly JwtConfig _jwtConfig = jwtConfig.Value;
        private readonly IUserService _userService = userService;
        private readonly ILogger<TokenService> _logger = logger;

        public List<Claim> CreateClaimForAccessToken(Core.Domain.Identity.User user, IList<string>? roles)
        {
            var authClaims = new List<Claim>()
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email), // de dung cho .net
                //new(JwtRegisteredClaimNames.Email, user.Email), // de dung cho frontend
                new(JwtRegisteredClaimNames.Sub, user.Id), // dung sub phai cau hinh them gi do de dung cho frontend
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
            };

            foreach (var role in roles)
            {
                authClaims.Add(new(ClaimTypes.Role, role));
            }

            return authClaims;
        }

        public string GenerateToken(IEnumerable<Claim> authClaims, double expiryInMinutes)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                expires: DateTime.Now.AddMinutes(expiryInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return jwtToken;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];

            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);

            // rng la 1 object
            // rng.GetBytes(randomNumber);
            // tao ra 1 byte roi insert vao randomNumber cho day mang
            // 1 byte la 8 bit day nhi phan 0 1
            // Convert.ToBase64String(randomNumber)
            // sau do chuyen cac byte thanh chuoi base64
            // vd "z5FNRt5T8BgMErkYkY8k/hv63M0+0UXrJxN4VtQO5iPjoxYtC4JccIhC5g=="
        }

        public ClaimsPrincipal GetPrincipalFromToken(string token, bool validateLifeTime)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _jwtConfig.Audience,
                ValidateIssuer = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey)),
                ValidateLifetime = validateLifeTime // bo qua het han de lay claims
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token không hợp lệ");

            return principal;
        }

        public async Task<TokenResponse> RefreshAccessToken(TokenRequest tokenRequest, string refreshToken)
        {
            _logger.LogInformation("Bat dau xu ly refresh token");
            if (tokenRequest is null)
            {
                _logger.LogWarning("Access token null");
                throw new Exception("Có lỗi xảy ra");
            }

            string accessToken = tokenRequest.AccessToken;

            // lay thong tin tu access token het han
            var principal = GetPrincipalFromToken(accessToken, false); // Access Token het han nen de la false

            if (principal == null || principal.Identity == null || principal.Identity.Name == null)
            {
                _logger.LogWarning("Principal tu access token bi null");
                throw new Exception("Có lỗi xảy ra");
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                _logger.LogDebug("Khong lay duoc user id tu principal");
                throw new Exception("Có lỗi xảy ra");
            }

            var user = await _userService.GetUserById(userId);

            if (user == null)
            {
                _logger.LogWarning("Khong tim thay user tuong ung với user id tu principal");
                throw new Exception("Có lỗi xảy ra");
            }

            if (user.RefreshToken != refreshToken)
            {
                _logger.LogWarning("Refresh token khong khop");
                throw new Exception("Có lỗi xảy ra");
            }

            if (user.RefreshTokenExpriryTime <= DateTime.Now)
            {
                _logger.LogWarning("Refresh token da het han");
                throw new Exception("Có lỗi xảy ra");
            }

            var roles = await _userService.GetUserRoles(user);

            var claims = CreateClaimForAccessToken(user, roles); // tao lai de tao moi ca jti cho an toan

            var newAccessToken = GenerateToken(claims, 1);

            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Refresh token thanh cong");

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            };
        }

        public async Task Revoke(string userId)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Khong tim thay user");
                throw new Exception("Có lỗi xảy ra");
            }

            user.RefreshToken = null;

            user.RefreshTokenExpriryTime = null;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
    }
}
