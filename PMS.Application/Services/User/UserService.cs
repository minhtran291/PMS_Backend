using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Auth;
using PMS.Application.DTOs.Customer;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.User
{
    public class UserService(IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        ILogger<UserService> logger, INotificationService notificationService) : Service(unitOfWork, mapper), IUserService
    {
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<UserService> _logger = logger;
        private readonly INotificationService _notificationService = notificationService;

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
                UserStatus = UserStatus.Inactive,
                Address = customer.Address,
                Avatar = "/images/AvatarDefault.png",
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

        public async Task<ServiceResult<bool>> UpdateCustomerProfile(string userId, CustomerProfileDTO request)
        {
            try
            {
                var exUser = await _unitOfWork.CustomerProfile.Query()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (exUser == null)
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        Message = "không tìm thấy userId hoặc đã bị khóa",
                        StatusCode = 200,
                    };
                }
                //
                var username = await _unitOfWork.Users.Query().FirstOrDefaultAsync(un => un.Id == exUser.UserId);
                if (username == null) { return new ServiceResult<bool> { Data = false, Message = "không tìm thấy userId hoặc đã bị khóa", StatusCode = 200 }; }
                //
                exUser.Mst = request.Mst;
                exUser.Mshkd = request.Mshkd;
                exUser.ImageCnkd = request.ImageCnkd;
                exUser.ImageByt = request.ImageByt;
                _unitOfWork.CustomerProfile.Update(exUser);
                await _unitOfWork.CommitAsync();
                //
                await _notificationService.SendNotificationToRolesAsync(
                    senderId: userId,
                    targetRoles: new List<string> { "ADMIN" },
                    title: "Thông báo duyệt tài khoản",
                    message: $"Khách hàng {username.UserName} đã cập nhật thông tin hồ sơ." +
                    $" Vui lòng kiểm tra và xác nhận.",
                    type: NotificationType.System
                );
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Data = true,
                    Message = "Thành công vui lòng chờ xác nhận của quản lý để mở khóa tài khoản"
                };

            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Lỗi khi cập nhật sản phẩm: {ex.Message}", ex);
            }

        }


        public async Task<ServiceResult<IEnumerable<CustomerDTO>>> GetAllCustomerWithInactiveStatus()
        {
            try
            {

                var listUser = await _unitOfWork.Users.Query()
                    .Include(u => u.CustomerProfile)
                    .Where(u => u.UserStatus == UserStatus.Inactive)
                    .ToListAsync();
                var result = listUser.Select(u => new CustomerDTO
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Mst = u.CustomerProfile?.Mst,
                    ImageCnkd = u.CustomerProfile?.ImageCnkd ?? string.Empty,
                    ImageByt = u.CustomerProfile?.ImageByt ?? string.Empty,
                    Mshkd = u.CustomerProfile?.Mshkd,
                    UserStatus = u.UserStatus
                }).ToList();
                return new ServiceResult<IEnumerable<CustomerDTO>>
                {
                    Data = result,
                    StatusCode = 200,
                    Message = result.Any()
                        ? "Lấy danh sách khách hàng thành công."
                        : "Không có khách hàng nào ở trạng thái Inactive."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<CustomerDTO>>
                {
                    Data = Enumerable.Empty<CustomerDTO>(),
                    StatusCode = 500,
                    Message = $"Đã xảy ra lỗi khi lấy danh sách khách hàng: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> UpdateCustomerStatus(string userId,string managerId)
        {
            var exuser = await _unitOfWork.Users.Query()
                    .Include(u => u.CustomerProfile).FirstOrDefaultAsync(u => u.Id == userId);
            if (exuser == null)
            {
                return new ServiceResult<bool> { Data = false, Message = "không tìm thấy người dùng", StatusCode = 200 };
            }
            exuser.UserStatus = UserStatus.Active;
            _unitOfWork.Users.Update(exuser);
            await _unitOfWork.CommitAsync();

            await _notificationService.SendNotificationToCustomerAsync(
                senderId: managerId,
                userId,
                title: "Thông báo duyệt tài khoản",
                message: $"tài khoản đã cập nhật ",
                type: NotificationType.System);

            return new ServiceResult<bool>
            {
                Data = true,
                Message = "cập nhật thành công",
                StatusCode = 200,
            };  
        }

        public async Task<ServiceResult<CustomerViewDTO>> GetCustomerByIdAsync(string userId)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(u => u.CustomerProfile)
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null || user.CustomerProfile == null)
            {
                return new ServiceResult<CustomerViewDTO>
                {
                    Data = null,
                    Message = "Không tìm thấy người dùng hoặc người dùng không có hồ sơ khách hàng.",
                    StatusCode = 404
                };
            }

            var dto = new CustomerViewDTO
            {
                // From User
                Id = user.Id,
                FullName = user.FullName,
                Avatar = user.Avatar,
                Address = user.Address,
                Gender = user.Gender,
                CreateAt = user.CreateAt,
                UserStatus = user.UserStatus,

                // From CustomerProfile
                CustomerProfileId = user.CustomerProfile.Id,
                Mst = user.CustomerProfile.Mst,
                ImageCnkd = user.CustomerProfile.ImageCnkd,
                ImageByt = user.CustomerProfile.ImageByt,
                Mshkd = user.CustomerProfile.Mshkd
            };

            return new ServiceResult<CustomerViewDTO>
            {
                Data = dto,
                Message = "Lấy thông tin khách hàng thành công.",
                StatusCode = 200
            };
        }


    }
}
