using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;
using PMS.Application.DTOs.Auth;

namespace PMS.Application.Services.User
{
    public class UserService(IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        ILogger<UserService> logger) : Service(unitOfWork, mapper), IUserService
    {
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<UserService> _logger = logger;

        public async Task<Core.Domain.Identity.User?> GetUserById(string userId)
        {
            return await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
        }

        public async Task<IList<string>> GetUserRoles(Core.Domain.Identity.User user)
        {
            return await _unitOfWork.Users.UserManager.GetRolesAsync(user);
        }

        public async Task<ServiceResult<bool>> RegisterUserAsync(RegisterUser customer)
        {
            var validateEmail = await _unitOfWork.Users.UserManager.FindByEmailAsync(customer.Email);

            if (validateEmail != null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Trùng email",
                    Data = false
                };
            }

            var validatePhone = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.PhoneNumber == customer.PhoneNumber);

            if (validatePhone != null)
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Trùng số điện thoại",
                    Data = false
                };

            var user = new Core.Domain.Identity.User
            {
                UserName = customer.UserName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                CreateAt = DateTime.Now,
                UserStatus = UserStatus.Active,
                Address = customer.Address,
                Avatar = "https://as2.ftcdn.net/v2/jpg/03/31/69/91/1000_F_331699188_lRpvqxO5QRtwOM05gR50ImaaJgBx68vi.jpg",
            };

            var createResult = await _unitOfWork.Users.UserManager.CreateAsync(user, customer.ConfirmPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Tao nguoi dung that bai: {Errors}", errors);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "có lỗi xảy ra",
                    Data = false
                };
            }

            var customerProfile = new Core.Domain.Entities.CustomerProfile
            {
                UserId = user.Id
            };
            await _unitOfWork.CustomerProfile.AddAsync(customerProfile);

            var roleResult = await _unitOfWork.Users.UserManager.AddToRoleAsync(user, UserRoles.CUSTOMER);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Gan role that bai: {Errors}", errors);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "có lỗi xảy ra",
                    Data = false
                };
            }

            await _unitOfWork.CommitAsync();

            await SendEmailConfirmAsync(user);
            _logger.LogInformation("Gui email xac nhan cho email: {Email}", user.Email);
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "Thành công vui lòng kiểm tra email",
                Data = true
            };

        }

        public async Task SendEmailConfirmAsync(Core.Domain.Identity.User user)
        {
            // tao token moi voi securiry stamp moi
            string token = await _unitOfWork.Users.UserManager.GenerateEmailConfirmationTokenAsync(user);

            UriBuilder uriBuilder = LinkConstant.UriBuilder("User", "confirm-email", user.Id, token);

            var link = uriBuilder.ToString();

            var body = EmailBody.CONFIRM_EMAIL(user.Email, link);

            await _emailService.SendMailAsync(EmailSubject.CONFIRM_EMAIL, body, user.Email);
        }

        public async Task<ServiceResult<bool>> ReSendEmailConfirmAsync(ResendConfirmEmailRequest request)
        {
            var user = await _unitOfWork.Users.UserManager.FindByEmailAsync(request.EmailOrUsername);
            if (user == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "sai email",
                    Data = false
                };
            }

            // cap nhat security stamp de vo hieu hoa cac token cu
            await _unitOfWork.Users.UserManager.UpdateSecurityStampAsync(user);

            await _unitOfWork.CommitAsync();

            await SendEmailConfirmAsync(user);
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "thành công vui lòng kiểm tra email",
                Data = true
            };
        }

        public async Task<ServiceResult<bool>> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
            if (user == null)
            {

                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy người dùng",
                    Data = false
                };
            }

            if (user.EmailConfirmed == true)
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Tài khoản đã được xác nhận trước đó",
                    Data = false
                };

            var confirmEmail = await _unitOfWork.Users.UserManager.ConfirmEmailAsync(user, token);

            if (!confirmEmail.Succeeded)
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Message = "Token không hợp lệ",
                    Data = false
                };

            await _unitOfWork.CommitAsync();
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "Thành công",
                Data = false
            };
        }

        public async Task<ServiceResult<bool>> SendEmailResetPasswordAsync(string email)
        {
            var isExisted = await _unitOfWork.Users.UserManager.FindByEmailAsync(email);
            if (isExisted == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy email",
                    Data = false
                };
            }

            var isConfirmed = await _unitOfWork.Users.UserManager.IsEmailConfirmedAsync(isExisted);

            if (!isConfirmed)

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Tài khoản chưa được xác nhận",
                    Data = false
                };

            // vo hieu hoa cac token trc
            await _unitOfWork.Users.UserManager.UpdateSecurityStampAsync(isExisted);

            // tao token moi voi security stamp moi
            var token = await _unitOfWork.Users.UserManager.GeneratePasswordResetTokenAsync(isExisted);

            UriBuilder uriBuilder = LinkConstant.UriBuilder("User", "reset-password", isExisted.Id, token);

            var link = uriBuilder.ToString();

            var body = EmailBody.RESET_PASSWORD(isExisted.Email, link);

            await _emailService.SendMailAsync(EmailSubject.RESET_PASSWORD, body, isExisted.Email);
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "Thành công vui lòng kiểm tra mail",
                Data = true
            };
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                _logger.LogWarning("Khong tim thay user id cua nguoi dung");
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy người dùng",
                    Data = false
                };
            }

            var result = await _unitOfWork.Users.UserManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Dat lai mat khau loi: {Erros}", errors);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "đặt lại mật khẩu thất bại",
                    Data = false
                };
            }

            await _unitOfWork.Users.UserManager.UpdateSecurityStampAsync(user);

            await _unitOfWork.CommitAsync();
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "thành công",
                Data = true
            };
        }
    }
}
