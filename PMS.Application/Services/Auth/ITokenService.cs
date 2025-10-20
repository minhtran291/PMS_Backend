using PMS.Application.DTOs.Auth;
using PMS.Core.Domain.Constant;
using System.Security.Claims;

namespace PMS.Application.Services.Auth
{
    public interface ITokenService
    {
        List<Claim> CreateClaimForAccessToken(Core.Domain.Identity.User user, IList<string> roles);
        string GenerateToken(IEnumerable<Claim> authClaims, double expiryInMinutes);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromToken(string token, bool validateLifeTime);
        Task<ServiceResult<TokenResponse>> RefreshAccessToken(TokenRequest tokenRequest, string refreshToken);
        Task <ServiceResult<object>>Revoke(string userId);
    }
}
