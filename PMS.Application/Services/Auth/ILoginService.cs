using PMS.Application.DTOs.Auth;

namespace PMS.Application.Services.Auth
{
    public interface ILoginService
    {
        public Task<TokenResponse> Login(LoginRequest request);
    }
}
