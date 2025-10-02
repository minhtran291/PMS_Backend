using AutoMapper;
using PMS.API.Services.Base;
using PMS.API.Services.User;
using PMS.Core.DTO.Auth;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.Auth
{
    public class LoginService(IUnitOfWork unitOfWork, IMapper mapper, ITokenService tokenService, IUserService userService) : Service(unitOfWork, mapper), ILoginService
    {
        private readonly ITokenService _tokenService = tokenService;
        private readonly IUserService _userService = userService;

        public async Task<TokenResponse> Login(LoginRequest request)
        {
            var account = (request.UsernameOrEmail!.Contains('@'))
                ? await _unitOfWork.Users.UserManager.FindByEmailAsync(request.UsernameOrEmail)
                : await _unitOfWork.Users.UserManager.FindByNameAsync(request.UsernameOrEmail)
                ?? throw new Exception("Username hoặc Email không tồn tại!");

            var checkPasswork = await _unitOfWork.Users.SignInManager.CheckPasswordSignInAsync(account, request.Password, false);

            if (!checkPasswork.Succeeded)
                throw new Exception("Mật khẩu không chính xác!");

            var roles = await _userService.GetUserRoles(account);

            if (roles == null || roles.Count == 0)
                throw new Exception("User chua co role");

            var authClaims = _tokenService.CreateClaimFromUser(account, roles);

            var accessToken = _tokenService.GenerateAccessToken(authClaims);

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            account.RefreshToken = newRefreshToken;

            account.RefreshTokenExpriryTime = DateTime.UtcNow.AddDays(10);

            await _unitOfWork.Users.UserManager.UpdateAsync(account);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
            };
        }
    }
}
