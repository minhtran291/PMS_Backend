using PMS.Application.DTOs.Auth;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.Auth
{
    public interface ILoginService
    {
        public Task<ServiceResult<TokenResponse>> Login(LoginRequest request);
    }
}
