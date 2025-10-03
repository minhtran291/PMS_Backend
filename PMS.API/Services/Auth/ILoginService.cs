using PMS.Core.DTO.Auth;

namespace PMS.API.Services.Auth
{
    public interface ILoginService
    {
        public Task<TokenResponse> Login(LoginRequest request);
    }
}
