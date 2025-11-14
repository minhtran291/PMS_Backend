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
using PMS.Core.Domain.Constant;

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

        public List<Claim> CreateClaimForAccessToken(Core.Domain.Identity.User user, IList<string> roles)
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

            if (user.CustomerProfile != null)
            {
                authClaims.Add(new Claim("customer_id", user.CustomerProfile.Id.ToString()));
            }

            if (user.StaffProfile != null)
            {
                authClaims.Add(new Claim("staff_id", user.StaffProfile.Id.ToString()));
            }

            foreach (var role in roles)
            {
                authClaims.Add(new(ClaimTypes.Role, role));
            }

            return authClaims;
        }

        public string GenerateToken(IEnumerable<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                expires: DateTime.Now.AddMinutes(_jwtConfig.ExpireInMinutes),
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

        public async Task<ServiceResult<TokenResponse>> RefreshAccessToken(TokenRequest tokenRequest, string refreshToken)
        {
            try
            {
                _logger.LogInformation("Bat dau xu ly refresh token");

                if (tokenRequest == null || string.IsNullOrEmpty(tokenRequest.AccessToken))
                {
                    _logger.LogInformation("Access token khong hop le");
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 400,
                    };
                }

                string accessToken = tokenRequest.AccessToken;

                // lay thong tin tu access token het han
                var principal = GetPrincipalFromToken(accessToken, false); // Access Token het han nen de la false

                if (principal == null || principal.Identity == null || principal.Identity.Name == null)
                {
                    _logger.LogInformation("Khong xac thuc duoc thong tin nguoi dung tu access token");
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 401,
                    };
                }

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("Khong lay duoc user id tu claim");
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 401,
                    };
                }

                var user = await _userService.GetUserById(userId);

                if (user == null)
                {
                    _logger.LogInformation("Nguoi dung khong ton tai");
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 404,
                    };
                }


                if (user.RefreshToken != refreshToken)
                {
                    _logger.LogInformation("Refresh token khong hop le");
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 401,
                    };
                }

                if (user.RefreshTokenExpriryTime <= DateTime.Now)
                {
                    _logger.LogInformation("Refresh token da het han");
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 401,
                    };
                }

                var roles = await _userService.GetUserRoles(user);

                var claims = CreateClaimForAccessToken(user, roles); // tao lai de tao moi ca jti cho an toan

                var newAccessToken = GenerateToken(claims);

                var newRefreshToken = GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;

                await _unitOfWork.Users.UserManager.UpdateAsync(user);

                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Refresh token thanh cong");

                return new ServiceResult<TokenResponse>
                {
                    StatusCode = 200,
                    Message = "",
                    Data = new TokenResponse
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<TokenResponse>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> Revoke(string userId)
        {
            try
            {
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogInformation("Khong tim thay user");
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                    };
                }

                user.RefreshToken = null;

                user.RefreshTokenExpriryTime = null;

                await _unitOfWork.Users.UserManager.UpdateAsync(user);

                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }
    }
}
