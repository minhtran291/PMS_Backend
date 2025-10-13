using PMS.Application.DTOs.Auth;
using System.Security.Claims;

namespace PMS.Application.Services.Auth
{
    public interface ITokenService
    {
        List<Claim> CreateClaimForAccessToken(Core.Domain.Identity.User user, IList<string>? roles);
        string GenerateToken(IEnumerable<Claim> authClaims, double expiryInMinutes);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromToken(string token, bool validateLifeTime);
        Task<TokenResponse> RefreshAccessToken(TokenRequest tokenRequest, string refreshToken);
        Task Revoke(string userId);
    }
}
