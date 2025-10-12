using AutoMapper;
using PMS.API.Services.Base;
using PMS.API.Services.User;
using PMS.Core.DTO.Auth;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.Auth
{
    public class LoginService(IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ITokenService tokenService, 
        IUserService userService, 
        ILogger<LoginService> logger) : Service(unitOfWork, mapper), ILoginService
    {
        private readonly ITokenService _tokenService = tokenService;
        private readonly IUserService _userService = userService;
        private readonly ILogger<LoginService> _logger = logger;

        public async Task<TokenResponse> Login(LoginRequest request)
        {
            var account = await _unitOfWork.Users.UserManager.FindByEmailAsync(request.UsernameOrEmail);

            if (account == null)
            {
                _logger.LogWarning("Email khong ton tai");
                throw new Exception("Email hoặc mật khẩu không chính xác");
            }

            if (account.UserStatus == Core.Domain.Enums.UserStatus.Block)
                throw new Exception("Tài khoản của bạn đã bị khóa");

            if (!account.EmailConfirmed)
                throw new Exception("Tài khoản chưa được xác nhận");

            var checkPasswork = await _unitOfWork.Users.SignInManager.CheckPasswordSignInAsync(account, request.Password, false);

            if (!checkPasswork.Succeeded)
            {
                _logger.LogWarning("Mat khau khong chinh xac");
                throw new Exception("Email hoặc mật khẩu không chính xác");
            }

            var roles = await _userService.GetUserRoles(account);

            if (roles == null || roles.Count == 0)
            {
                _logger.LogWarning("Khong lay duoc role cua user");
                throw new Exception("Có lỗi xảy ra");
            }

            var authClaims = _tokenService.CreateClaimForAccessToken(account, roles);
            var accessToken = _tokenService.GenerateToken(authClaims, 5);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            account.RefreshToken = newRefreshToken;
            account.RefreshTokenExpriryTime = DateTime.Now.AddDays(10);
            await _unitOfWork.Users.UserManager.UpdateAsync(account);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
            };
        }
    }
}
