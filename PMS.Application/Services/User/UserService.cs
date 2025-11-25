using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
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
        ILogger<UserService> logger, INotificationService notificationService, IWebHostEnvironment webEnv) : Service(unitOfWork, mapper), IUserService
    {
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<UserService> _logger = logger;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IWebHostEnvironment _webEnv = webEnv;

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
            _logger.LogWarning($"=== BẮT ĐẦU ĐĂNG KÝ USER ===");
            _logger.LogInformation($"Bắt đầu đăng ký user: {customer.Email}");

            var validateEmail = await _unitOfWork.Users.UserManager.FindByEmailAsync(customer.Email);
            _logger.LogInformation($"Kết quả tìm email: {validateEmail?.Email} - EmailConfirmed: {validateEmail?.EmailConfirmed}");

            if (validateEmail != null)
            {
                _logger.LogWarning($"=== EMAIL ĐÃ TỒN TẠI ===");
                _logger.LogWarning($"Email: {validateEmail.Email}");
                _logger.LogWarning($"EmailConfirmed: {validateEmail.EmailConfirmed}");

                // Kiểm tra email đã được xác thực hay chưa
                if (validateEmail.EmailConfirmed)
                {
                    _logger.LogWarning($"Email đã được đăng ký: {customer.Email}");
                    _logger.LogWarning($"Trả về StatusCode 400 với message: Email đã được đăng ký");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Email đã được đăng ký",
                        Data = false
                    };
                }
                else
                {
                    _logger.LogWarning($"Email chưa xác thực: {customer.Email}");
                    _logger.LogWarning($"Trả về StatusCode 400 với message: Email chưa xác thực");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Email chưa xác thực",
                        Data = false
                    };
                }
            }

            _logger.LogInformation($"Email không tồn tại, tiếp tục đăng ký: {customer.Email}");

            var validatePhone = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.PhoneNumber == customer.PhoneNumber);

            if (validatePhone != null)
            {
                _logger.LogWarning($"Số điện thoại đã tồn tại: {customer.PhoneNumber}");
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Message = "Trùng số điện thoại",
                    Data = false
                };
            }

            var validateUsername = await _unitOfWork.Users.UserManager.FindByNameAsync(customer.UserName.ToLower());

            if (validateUsername != null)
            {
                _logger.LogWarning($"Tên đăng nhập đã tồn tại: {customer.UserName}");
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Message = "Tên đăng nhập đã tồn tại",
                    Data = false
                };
            }

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

            _logger.LogWarning($"=== TẠO USER ===");
            _logger.LogWarning($"UserName: {user.UserName}");
            _logger.LogWarning($"Email: {user.Email}");
            _logger.LogWarning($"PhoneNumber: {user.PhoneNumber}");
            _logger.LogWarning($"Password length: {customer.ConfirmPassword?.Length}");

            var createResult = await _unitOfWork.Users.UserManager.CreateAsync(user, customer.ConfirmPassword);
            _logger.LogWarning($"CreateResult.Succeeded: {createResult.Succeeded}");
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Tạo tài khoản thất bại: {Errors}", errors);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra",
                    Data = false
                };
            }
            _logger.LogWarning($"Tạo user thành công: {user.Email} - ID: {user.Id}");

            var customerProfile = new Core.Domain.Entities.CustomerProfile
            {
                UserId = user.Id
            };
            await _unitOfWork.CustomerProfile.AddAsync(customerProfile);
            _logger.LogWarning($"Tạo CustomerProfile thành công cho user: {user.Id}");

            var roleResult = await _unitOfWork.Users.UserManager.AddToRoleAsync(user, UserRoles.CUSTOMER);
            _logger.LogWarning($"=== GÁN ROLE ===");
            _logger.LogWarning($"RoleResult.Succeeded: {roleResult.Succeeded}");
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Gán role thất bại: {Errors}", errors);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra",
                    Data = false
                };
            }
            _logger.LogWarning($"Gán role CUSTOMER thành công cho user: {user.Id}");

            _logger.LogWarning($"=== COMMIT TRANSACTION ===");
            try
            {
                var result = await _unitOfWork.CommitAsync();
                _logger.LogWarning($"Commit transaction thành công - Rows affected: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Commit transaction thất bại: {ex.Message}");
                _logger.LogError($"Exception details: {ex}");
                throw;
            }

            _logger.LogWarning($"=== GỬI EMAIL ===");
            try
            {
                await SendEmailConfirmAsync(user);
                _logger.LogInformation("Gửi email xác nhận thành công: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Gửi email thất bại: {ex.Message}");
                // Không throw exception ở đây vì user đã được tạo thành công
            }
            _logger.LogWarning($"=== ĐĂNG KÝ THÀNH CÔNG ===");
            _logger.LogWarning($"Email: {user.Email}");
            _logger.LogWarning($"Trả về StatusCode 200 với message: Thành công vui lòng kiểm tra email");
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "Thành công vui lòng kiểm tra email",
                Data = true
            };

        }

        public async Task SendEmailConfirmAsync(Core.Domain.Identity.User user)
        {
            _logger.LogWarning($"=== BẮT ĐẦU GỬI EMAIL ===");
            _logger.LogWarning($"User Email: {user.Email}");
            _logger.LogWarning($"User ID: {user.Id}");

            // tao token moi voi securiry stamp moi
            string token = await _unitOfWork.Users.UserManager.GenerateEmailConfirmationTokenAsync(user);
            _logger.LogWarning($"Token generated: {token?.Substring(0, Math.Min(20, token?.Length ?? 0))}...");

            UriBuilder uriBuilder = LinkConstant.UriBuilder("User", "confirm-email", user.Id, token);

            var link = uriBuilder.ToString();
            _logger.LogWarning($"Link generated: {link}");

            var body = EmailBody.CONFIRM_EMAIL(user.Email, link);
            _logger.LogWarning($"Email body generated");

            _logger.LogWarning($"=== GỬI EMAIL QUA SERVICE ===");
            try
            {
                _logger.LogWarning($"Calling SendMailAsync with subject: {EmailSubject.CONFIRM_EMAIL}");
                _logger.LogWarning($"Body length: {body?.Length}");
                _logger.LogWarning($"To email: {user.Email}");

                await _emailService.SendMailAsync(EmailSubject.CONFIRM_EMAIL, body, user.Email);
                _logger.LogWarning($"Email sent successfully to: {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email service error: {ex.Message}");
                _logger.LogError($"Exception details: {ex}");
                throw;
            }
        }

        public async Task<ServiceResult<bool>> ReSendEmailConfirmAsync(ResendConfirmEmailRequest request)
        {
            var user = (request.EmailOrUsername!.Contains('@'))
                ? await _unitOfWork.Users.UserManager.FindByEmailAsync(request.EmailOrUsername)
                : await _unitOfWork.Users.UserManager.FindByNameAsync(request.EmailOrUsername);
            if (user == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "Sai email",
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
                Message = "Thành công, vui lòng kiểm tra email",
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
                    Message = "Tài khoản đã được xác nhận thành công",
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
                Message = "Thành công vui lòng kiểm tra email",
                Data = true
            };
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy user id của người dùng");
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
                _logger.LogWarning("Đặt lại mật khẩu lỗi: {Erros}", errors);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Đặt lại mật khẩu thất bại",
                    Data = false
                };
            }

            await _unitOfWork.Users.UserManager.UpdateSecurityStampAsync(user);

            await _unitOfWork.CommitAsync();
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "Đặt lại mật khẩu thành công",
                Data = true
            };
        }

        public async Task<ServiceResult<bool>> UpdateCustomerProfile(string userId, CustomerProfileDTO request)
        {
               await _unitOfWork.BeginTransactionAsync();

            try
            {
                var profile = await _unitOfWork.CustomerProfile
                    .Query()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (profile == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy hồ sơ khách hàng.", 404);

                var user = await _unitOfWork.Users.Query()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy tài khoản người dùng.", 404);

                
                string cnkdUrl = request.ImageCnkd != null
             ? await SaveAsync(request.ImageCnkd, "customer/cnkd/")
             : profile.ImageCnkd;
                string bytUrl = request.ImageByt != null
            ? await SaveAsync(request.ImageByt, "customer/byt/")
            : profile.ImageByt;

              
                profile.Mst = request.Mst;
                profile.Mshkd = request.Mshkd;
                profile.ImageCnkd = cnkdUrl;
                profile.ImageByt = bytUrl;

                _unitOfWork.CustomerProfile.Update(profile);
                await _unitOfWork.CommitAsync();

                
                await _notificationService.SendNotificationToRolesAsync(
                    senderId: userId,
                    targetRoles: new List<string> { "ADMIN" },
                    title: "Yêu cầu duyệt hồ sơ",
                    message: $"Khách hàng {user.UserName} có id {user.Id} vừa cập nhật hồ sơ và cần duyệt.",
                    type: NotificationType.System
                );

                await _notificationService.SendNotificationToRolesAsync(
                    senderId: userId,
                    targetRoles: new List<string> { "MANAGER" },
                    title: "Yêu cầu duyệt hồ sơ",
                    message: $"Khách hàng {user.UserName} có id {user.Id} vừa cập nhật hồ sơ và cần duyệt.",
                    type: NotificationType.System
                );

                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<bool>.SuccessResult(
                    true,
                    "Cập nhật hồ sơ thành công. Vui lòng chờ quản lý duyệt."
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<bool>.Fail($"Lỗi khi cập nhật hồ sơ: {ex.Message}", 500);
            }

        }

        public async Task<string> SaveAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File upload không hợp lệ");

           
            var rootPath = _webEnv.WebRootPath;

            
            var folderPath = Path.Combine(rootPath, folder);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            return $"/{folder}/{fileName}".Replace("\\", "/");
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

        public async Task<ServiceResult<bool>> UpdateCustomerStatus(string userId, string verifier)
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
                senderId: verifier,
                userId,
                title: "Thông báo duyệt tài khoản",
                message: $"Tài khoản đã cập nhật ",
                type: NotificationType.System);

            return new ServiceResult<bool>
            {
                Data = true,
                Message = "Cập nhật thành công",
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

        public async Task<ServiceResult<bool>> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        Message = $"Không tìm thấy người dùng với ID: {userId}",
                        StatusCode = 404
                    };
                }


                var result = await _unitOfWork.Users.UserManager.ChangePasswordAsync(user, oldPassword, newPassword);
                if (!result.Succeeded)
                {

                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        Message = $"Đổi mật khẩu thất bại: {errors}",
                        StatusCode = 400
                    };
                }

                return new ServiceResult<bool>
                {
                    Data = true,
                    Message = "Đổi mật khẩu thành công.",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error changing password: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<object>> GetProfile(string userId, List<string> roles)
        {
            try
            {
                var result = await GetProfileByRoleAsync(userId, roles);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result,
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        private async Task<object> GetProfileByRoleAsync(string userId, List<string> roles)
        {
            var query = _unitOfWork.Users.Query();

            if (roles.Contains(UserRoles.SALES_STAFF) || roles.Contains(UserRoles.ACCOUNTANT) 
                || roles.Contains(UserRoles.PURCHASES_STAFF) || roles.Contains(UserRoles.WAREHOUSE_STAFF))
            {
                var staff = await query.Include(u => u.StaffProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                return _mapper.Map<DTOs.Profile.StaffProfileDTO>(staff);
            }

            if (roles.Contains(UserRoles.CUSTOMER))
            {
                var customer = await query.Include(u => u.CustomerProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                return _mapper.Map<DTOs.Profile.CustomerProfileDTO>(customer);
            }

            var common = await query.FirstOrDefaultAsync(u => u.Id == userId);
            return _mapper.Map<DTOs.Profile.CommonProfileDTO>(common);
        }

        public async Task<ServiceResult<CustomerStatusDTO>> GetCustomerStatusAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Kiểm tra trạng thái customer: {userId}");

                // Kiểm tra user có tồn tại không
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Không tìm thấy user với ID: {userId}");
                    return new ServiceResult<CustomerStatusDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng",
                        Data = null
                    };
                }

                // Kiểm tra user có phải customer không
                var roles = await _unitOfWork.Users.UserManager.GetRolesAsync(user);
                if (!roles.Contains(UserRoles.CUSTOMER))
                {
                    _logger.LogWarning($"User {userId} không phải customer");
                    return new ServiceResult<CustomerStatusDTO>
                    {
                        StatusCode = 403,
                        Message = "Chỉ customer mới có thể kiểm tra trạng thái",
                        Data = null
                    };
                }

                // Kiểm tra customer profile
                var customerProfile = await _unitOfWork.CustomerProfile.Query()
                    .FirstOrDefaultAsync(cp => cp.UserId == userId);

                var hasAdditionalInfo = customerProfile != null &&
                                       customerProfile.Mst.HasValue &&
                                       customerProfile.Mshkd.HasValue &&
                                       !string.IsNullOrEmpty(customerProfile.ImageCnkd);

                var needsAdditionalInfo = !hasAdditionalInfo;

                var statusDTO = new CustomerStatusDTO
                {
                    UserStatus = user.UserStatus,
                    HasAdditionalInfo = hasAdditionalInfo,
                    NeedsAdditionalInfo = needsAdditionalInfo,
                    Message = user.UserStatus switch
                    {
                        UserStatus.Block => "Tài khoản của bạn đã bị khóa",
                        UserStatus.Inactive => hasAdditionalInfo ? "Thông tin của bạn đang chờ manager duyệt" : "Bạn cần bổ sung thông tin mã số thuế và mã số kinh doanh",
                        UserStatus.Active => "Tài khoản của bạn đã được kích hoạt",
                        _ => "Trạng thái không xác định"
                    }
                };

                _logger.LogInformation($"Trạng thái customer {userId}: {statusDTO.UserStatus}, HasAdditionalInfo: {statusDTO.HasAdditionalInfo}");
                return new ServiceResult<CustomerStatusDTO>
                {
                    StatusCode = 200,
                    Message = "Lấy trạng thái thành công",
                    Data = statusDTO
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kiểm tra trạng thái customer: {userId}");
                return new ServiceResult<CustomerStatusDTO>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi kiểm tra trạng thái",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<bool>> SubmitCustomerAdditionalInfoAsync(string userId, CustomerAdditionalInfoDTO additionalInfo)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu submit thông tin bổ sung cho customer: {userId}");

                // Kiểm tra user có tồn tại không
                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Không tìm thấy user với ID: {userId}");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy người dùng",
                        Data = false
                    };
                }

                // Kiểm tra user có phải customer không
                var roles = await _unitOfWork.Users.UserManager.GetRolesAsync(user);
                if (!roles.Contains(UserRoles.CUSTOMER))
                {
                    _logger.LogWarning($"User {userId} không phải customer");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 403,
                        Message = "Chỉ customer mới có thể submit thông tin bổ sung",
                        Data = false
                    };
                }

                // Kiểm tra customer profile có tồn tại không
                var customerProfile = await _unitOfWork.CustomerProfile.Query()
                    .FirstOrDefaultAsync(cp => cp.UserId == userId);

                if (customerProfile == null)
                {
                    _logger.LogWarning($"Không tìm thấy customer profile cho user: {userId}");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy thông tin customer",
                        Data = false
                    };
                }

                // Kiểm tra customer đã submit thông tin bổ sung chưa
                if (customerProfile.Mst.HasValue && customerProfile.Mshkd.HasValue)
                {
                    _logger.LogWarning($"Customer {userId} đã submit thông tin bổ sung");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Bạn đã submit thông tin bổ sung rồi",
                        Data = false
                    };
                }

                // Validate MST và MSHKD format
                if (additionalInfo.Mst <= 0 || additionalInfo.Mst.ToString().Length != 10)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Mã số thuế phải có đúng 10 chữ số",
                        Data = false
                    };
                }

                if (additionalInfo.Mshkd <= 0 || additionalInfo.Mshkd.ToString().Length != 10)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Mã số kinh doanh phải có đúng 10 chữ số",
                        Data = false
                    };
                }

                // Validate address
                if (string.IsNullOrWhiteSpace(additionalInfo.Address))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Địa chỉ không được để trống",
                        Data = false
                    };
                }

                if (additionalInfo.Address.Length > 256)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Địa chỉ không được vượt quá 256 ký tự",
                        Data = false
                    };
                }

                // Validate ImageCnkd
                if (string.IsNullOrWhiteSpace(additionalInfo.ImageCnkd))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Ảnh chứng nhận kinh doanh là bắt buộc",
                        Data = false
                    };
                }

                // Cập nhật thông tin bổ sung
                customerProfile.Mst = additionalInfo.Mst;
                customerProfile.Mshkd = additionalInfo.Mshkd;
                customerProfile.ImageCnkd = additionalInfo.ImageCnkd;
                // Không cập nhật ImageByt nữa

                // Cập nhật địa chỉ của user
                user.Address = additionalInfo.Address;

                // Cập nhật trạng thái user thành Inactive để chờ manager duyệt
                user.UserStatus = UserStatus.Inactive;

                _unitOfWork.CustomerProfile.Update(customerProfile);
                await _unitOfWork.Users.UserManager.UpdateAsync(user);
                await _unitOfWork.CommitAsync();

                // Gửi thông báo cho manager
                await _notificationService.SendNotificationToRolesAsync(
                    userId, // senderId
                    new List<string> { UserRoles.MANAGER }, // targetRoles
                    "Yêu cầu duyệt thông tin customer",
                    $"Customer {user.UserName} đã submit thông tin bổ sung và cần được duyệt",
                    NotificationType.System
                );

                _logger.LogInformation($"Submit thông tin bổ sung thành công cho customer: {userId}");
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Submit thông tin bổ sung thành công. Vui lòng chờ manager duyệt.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi submit thông tin bổ sung cho customer: {userId}");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi submit thông tin bổ sung",
                    Data = false
                };
            }
        }
    }
}

