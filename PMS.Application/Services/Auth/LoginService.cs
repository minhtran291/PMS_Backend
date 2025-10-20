using AutoMapper;
using PMS.Application.Services.User;
using PMS.Application.DTOs.Auth;
using PMS.Data.UnitOfWork;
using PMS.Application.Services.Base;
using Microsoft.Extensions.Logging;
using PMS.Core.Domain.Constant;
using Microsoft.EntityFrameworkCore;

namespace PMS.Application.Services.Auth
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

        public async Task<ServiceResult<TokenResponse>> Login(LoginRequest request)
        {
            try
            {
                var account = (request.UsernameOrEmail!.Contains('@'))
                ? await _unitOfWork.Users.UserManager.FindByEmailAsync(request.UsernameOrEmail)
                : await _unitOfWork.Users.UserManager.FindByNameAsync(request.UsernameOrEmail);

                if (account == null)
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 400,
                        Message = "Tài khoản không tồn tại"
                    };

                if (!account.EmailConfirmed)
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 400,
                        Message = "Tài khoản chưa được xác nhận"
                    };

                if (account.UserStatus == Core.Domain.Enums.UserStatus.Block)
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 400,
                        Message = "Tài khoản của bạn đã bị khóa"
                    };

                var checkPasswork = await _unitOfWork.Users.SignInManager.CheckPasswordSignInAsync(account, request.Password, false);

                if (!checkPasswork.Succeeded)
                    return new ServiceResult<TokenResponse>
                    {
                        StatusCode = 400,
                        Message = "Email hoặc mật khẩu không chính xác"
                    };

                var roles = await _userService.GetUserRoles(account);

                if (roles == null || roles.Count == 0)
                    throw new Exception("Khong lay duoc role cua user");

                if (roles.Contains(UserRoles.CUSTOMER))
                {
                    account = await _unitOfWork.Users
                        .Query()
                        .Include(u => u.CustomerProfile)
                        .FirstOrDefaultAsync(u => u.Id == account.Id);
                }

                if (roles.Any(r =>
                    r == UserRoles.SALES_STAFF ||
                    r == UserRoles.PURCHASES_STAFF ||
                    r == UserRoles.WAREHOUSE_STAFF ||
                    r == UserRoles.ACCOUNTANT))
                {
                    account = await _unitOfWork.Users
                        .Query()
                        .Include(a => a.StaffProfile)
                        .FirstOrDefaultAsync(u => u.Id == account.Id);
                }

                var authClaims = _tokenService.CreateClaimForAccessToken(account, roles);
                var accessToken = _tokenService.GenerateToken(authClaims, 1);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                account.RefreshToken = newRefreshToken;
                account.RefreshTokenExpriryTime = DateTime.Now.AddDays(10);
                await _unitOfWork.Users.UserManager.UpdateAsync(account);

                return new ServiceResult<TokenResponse> 
                {
                    StatusCode = 200,
                    Data = new TokenResponse
                    {
                        AccessToken = accessToken,
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
    }
}
