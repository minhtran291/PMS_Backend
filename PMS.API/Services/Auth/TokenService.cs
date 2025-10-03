using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PMS.API.Services.Base;
using PMS.API.Services.User;
using PMS.Core.ConfigOptions;
using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Auth;
using PMS.Data.UnitOfWork;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PMS.API.Services.Auth
{
    public class TokenService(IUnitOfWork unitOfWork, IMapper mapper, IOptions<JwtConfig> jwtConfig, IUserService userService) : Service(unitOfWork, mapper), ITokenService
    {
        private readonly JwtConfig _jwtConfig = jwtConfig.Value;
        private readonly IUserService _userService = userService;

        public List<Claim> CreateClaimFromUser(Core.Domain.Identity.User user, IList<string>? roles)
        {
            var authClaims = new List<Claim>()
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Sub, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
            };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            return authClaims;
        }

        public string GenerateAccessToken(IEnumerable<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpireInMinutes),
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

        public ClaimsPrincipal GetPrincipalFromExpriredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _jwtConfig.Audience,
                ValidateIssuer = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey)),
                ValidateLifetime = false // bo qua het han de lay claims
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        public async Task<TokenResponse> RefreshAccessToken(TokenRequest tokenRequest, string refreshToken)
        {
            if (tokenRequest is null)
                throw new Exception("Access token is null.");

            string accessToken = tokenRequest.AccessToken;

            var principal = GetPrincipalFromExpriredToken(accessToken);

            if (principal == null || principal.Identity == null || principal.Identity.Name == null)
                throw new Exception("Invalid principal.");

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Exception("Invalid token: missing user id.");

            var user = await _userService.GetUserById(userId)
                ?? throw new Exception("User is null.");

            if (user.RefreshToken != refreshToken)
                throw new Exception("Invalid client request.");

            if (user.RefreshTokenExpriryTime <= DateTime.UtcNow)
                throw new Exception("Refresh token has expired.");

            var roles = await _userService.GetUserRoles(user);

            var claims = CreateClaimFromUser(user, roles); // tao lai de tao moi ca jti cho an toan

            var newAccessToken = GenerateAccessToken(claims);

            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);

            await _unitOfWork.CommitAsync();

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            };
        }

        public async Task Revoke(string userId)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                ?? throw new Exception("User not found");

            user.RefreshToken = null;

            user.RefreshTokenExpriryTime = null;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
    }
}
