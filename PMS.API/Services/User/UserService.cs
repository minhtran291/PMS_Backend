using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.API.Services.Base;
using PMS.API.Services.ExternalService;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Core.DTO.Content;
using PMS.Core.DTO.Request;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.User
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

        public async Task RegisterUserAsync(RegisterUser customer)
        {
            var validateEmail = await _unitOfWork.Users.UserManager.FindByEmailAsync(customer.Email);

            if (validateEmail != null)
                throw new Exception("Email đã được sử dụng");

            var validatePhone = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.PhoneNumber == customer.PhoneNumber);

            if (validatePhone != null)
                throw new Exception("Số điện thoại đã được sử dụng");

            var user = new Core.Domain.Identity.User
            {
                UserName = customer.UserName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                CreateAt = DateTime.Now,
                UserStatus = UserStatus.Active
            };

            var createResult = await _unitOfWork.Users.UserManager.CreateAsync(user, customer.ConfirmPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Tao nguoi dung that bai: {Errors}", errors);
                throw new Exception("Có lỗi xảy ra");
            }

            var profile = new Core.Domain.Entities.Profile
            {
                UserId = user.Id,
                Address = customer.Address,
                Avatar = "https://as2.ftcdn.net/v2/jpg/03/31/69/91/1000_F_331699188_lRpvqxO5QRtwOM05gR50ImaaJgBx68vi.jpg",
                Gender = Gender.Other,
            };
            await _unitOfWork.Profile.AddAsync(profile);

            var customerProfile = new Core.Domain.Entities.CustomerProfile
            {
                ProfileId = profile.Id
            };
            await _unitOfWork.CustomerProfile.AddAsync(customerProfile);

            var roleResult = await _unitOfWork.Users.UserManager.AddToRoleAsync(user, UserRoles.CUSTOMER);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Gan role that bai: {Errors}", errors);
                throw new Exception("Có lỗi xảy ra");
            }

            await _unitOfWork.CommitAsync();

            await SendEmailConfirmAsync(user);
            _logger.LogInformation("Gui email xac nhan cho email: {Email}", user.Email);
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

        public async Task ReSendEmailConfirmAsync(ResendConfirmEmailRequest request)
        {
            var user = await _unitOfWork.Users.UserManager.FindByEmailAsync(request.EmailOrUsername) 
                ?? throw new Exception("Không nhận được email để gửi");

            // cap nhat security stamp de vo hieu hoa cac token cu
            await _unitOfWork.Users.UserManager.UpdateSecurityStampAsync(user);

            await _unitOfWork.CommitAsync();

            await SendEmailConfirmAsync(user);
        }

        public async Task ConfirmEmailAsync(string userId, string token)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                ?? throw new Exception("không tìm thấy người dùng");

            if (user.EmailConfirmed == true)
                throw new Exception("Tài khoản đã được xác nhận");

            var confirmEmail = await _unitOfWork.Users.UserManager.ConfirmEmailAsync(user, token);

            if (!confirmEmail.Succeeded)
                throw new Exception("Token không hợp lệ");

            await _unitOfWork.CommitAsync();
        }

        public async Task SendEmailResetPasswordAsync(string email)
        {
            var isExisted = await _unitOfWork.Users.UserManager.FindByEmailAsync(email) 
                ?? throw new Exception("Không tìm thấy Email");

            var isConfirmed = await _unitOfWork.Users.UserManager.IsEmailConfirmedAsync(isExisted);

            if (!isConfirmed)
                throw new Exception("Tài khoản chưa được xác nhận");

            // vo hieu hoa cac token trc
            await _unitOfWork.Users.UserManager.UpdateSecurityStampAsync(isExisted);

            // tao token moi voi security stamp moi
            var token = await _unitOfWork.Users.UserManager.GeneratePasswordResetTokenAsync(isExisted);

            UriBuilder uriBuilder = LinkConstant.UriBuilder("User", "reset-password", isExisted.Id, token);

            var link = uriBuilder.ToString();

            var body = EmailBody.RESET_PASSWORD(isExisted.Email, link);

            await _emailService.SendMailAsync(EmailSubject.RESET_PASSWORD, body, isExisted.Email);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                _logger.LogWarning("Khong tim thay user id cua nguoi dung");
                throw new Exception("Không tìm thấy người dùng");
            }

            var result = await _unitOfWork.Users.UserManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Dat lai mat khau loi: {Erros}", errors);
                throw new Exception("Đặt lại mật khẩu thất bại");
            }

            await _unitOfWork.Users.UserManager.UpdateSecurityStampAsync(user);

            await _unitOfWork.CommitAsync();
        }
    }
}
