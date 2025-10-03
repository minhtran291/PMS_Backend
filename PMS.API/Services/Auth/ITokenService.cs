using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Auth;
using System.Security.Claims;

namespace PMS.API.Services.Auth
{
    public interface ITokenService
    {
        public List<Claim> CreateClaimFromUser(Core.Domain.Identity.User user, IList<string>? roles);
        public string GenerateAccessToken(IEnumerable<Claim> authClaims);
        public string GenerateRefreshToken();
        public ClaimsPrincipal GetPrincipalFromExpriredToken(string token);
        public Task<TokenResponse> RefreshAccessToken(TokenRequest tokenRequest, string refreshToken);
        public Task Revoke(string userId);
    }
}
