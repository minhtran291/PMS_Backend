using AutoMapper;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Application.DTOs.Admin;
using PMS.Application.Services.Base;
using PMS.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using PMS.Core.Domain.Constant;
using Microsoft.Extensions.Logging;

namespace PMS.Application.Services.Admin
{
    public class AdminService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AdminService> logger) : Service(unitOfWork, mapper), IAdminService
    {
        private readonly ILogger<AdminService> _logger = logger;

        public async Task<ServiceResult<bool>> CreateAccountAsync(CreateAccountRequest request)
        {
            var validateEmail = await _unitOfWork.Users.UserManager.FindByEmailAsync(request.Email);

            if (validateEmail != null)
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Email đã được sử dụng",
                    Data = false
                };

            var validatePhone = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (validatePhone != null)
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Số điện thoại đã được sử dụng",
                    Data = false
                };
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var user = new Core.Domain.Identity.User
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    CreateAt = DateTime.Now,
                    UserStatus = UserStatus.Active,
                    EmailConfirmed = true,
                    FullName = request.FullName,
                    Address = request.Address,
                    Avatar = "/images/AvatarDefault.png",
                };

                var createResult = await _unitOfWork.Users.UserManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("Tao nguoi dung that bai: {Errors}", errors);
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "Có lỗi xảy ra",
                        Data = false
                    };
                }

                // Tạo StaffProfile
                string role = request.StaffRole switch
                {
                    StaffRole.SalesStaff => UserRoles.SALES_STAFF,
                    StaffRole.PurchasesStaff => UserRoles.PURCHASES_STAFF,
                    StaffRole.WarehouseStaff => UserRoles.WAREHOUSE_STAFF,
                    StaffRole.Accountant => UserRoles.ACCOUNTANT,
                    _ => throw new Exception("Role không hợp lệ")

                };

                var staffProfile = new StaffProfile
                {
                    UserId = user.Id,
                    EmployeeCode = string.IsNullOrWhiteSpace(request.EmployeeCode)
                        ? GenerateEmployeeCode(role)
                        : request.EmployeeCode,
                    Notes = request.Notes
                };
                await _unitOfWork.StaffProfile.AddAsync(staffProfile);

               
                var roleResult = await _unitOfWork.Users.UserManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogError("Gan role that bai: {Errors}", errors);
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "Có lỗi xảy ra",
                        Data = false
                    };
                }

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Tạo thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Tao nhan vien that bai");
                throw;
            }
        }

        public async Task<ServiceResult<AccountDetails>> GetAccountDetailAsync(string userId)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(p => p.StaffProfile)
                .Include(p => p.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new ServiceResult<AccountDetails>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy người dùng",
                    Data = null
                };
            }

            var roles = await _unitOfWork.Users.UserManager.GetRolesAsync(user);

            var data = new AccountDetails
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                UserStatus = user.UserStatus,
                CreateAt = user.CreateAt,

                FullName = user.FullName,
                Avatar = user.Avatar,
                Gender = user.Gender,
                Address = user.Address,

                StaffProfileId = user.StaffProfile?.Id,
                EmployeeCode = user.StaffProfile?.EmployeeCode,
                Notes = user.StaffProfile?.Notes,

                CustomerProfileId = user.CustomerProfile?.Id,
                Mst = user.CustomerProfile?.Mst,
                ImageCnkd = user.CustomerProfile?.ImageCnkd,
                ImageByt = user.CustomerProfile?.ImageByt,
                Mshkd = user.CustomerProfile?.Mshkd
            };

            return new ServiceResult<AccountDetails>
            {
                StatusCode = 200,
                Message = "Lấy thông tin tài khoản thành công",
                Data = data
            };
        }

        public async Task<List<AccountList>> GetAccountListAsync(string? keyword)
        {
            var users = _unitOfWork.Users.Query()
                    .Include(u => u.StaffProfile)
                    .Include(u => u.CustomerProfile)
                    .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                var handleKeyword = keyword.Trim().ToLower();

                users = users.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(handleKeyword)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(handleKeyword)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(handleKeyword)) ||
                    (u.StaffProfile != null &&
                     u.StaffProfile.EmployeeCode != null &&
                     u.StaffProfile.EmployeeCode.ToLower().Contains(handleKeyword)));
            }

            var result = await users.OrderByDescending(u => u.CreateAt).ToListAsync();

            if (result == null || result.Count == 0)
                throw new Exception("Không có dữ liệu");

            return result.Select(u => new AccountList
            {
                UserId = u.Id,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                UserStatus = u.UserStatus,
                CreateAt = u.CreateAt,
                FullName = u.FullName,
                Gender = u.Gender,
                IsCustomer = u.CustomerProfile != null
            }).ToList();
        }

        public async Task<ServiceResult<bool>> SuspendAccountAsync(string userId)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "không tìm thấy user",
                    Data = false
                };
            }

            user.UserStatus = UserStatus.Block;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "cập nhật thành công",
                Data = true
            };
        }

        public async Task<ServiceResult<bool>> UpdateAccountAsync(UpdateAccountRequest request)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(u => u.StaffProfile)
                .FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "không tìm thấy user",
                    Data = false
                };
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Update User
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    user.PhoneNumber = request.PhoneNumber;

                if (request.UserStatus.HasValue)
                    user.UserStatus = request.UserStatus.Value;

                if (request.FullName != null) user.FullName = request.FullName;
                if (request.Avatar != null) user.Avatar = request.Avatar;
                user.Gender = request.Gender;
                if (request.Address != null) user.Address = request.Address;

                await _unitOfWork.Users.UserManager.UpdateAsync(user);

                // Update / Upsert StaffProfile
                var staffProfile = user.StaffProfile;
                if (staffProfile == null)
                {
                    _logger.LogWarning("Update profile loi, khong co staff profile");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "có lỗi xảy ra",
                        Data = false
                    };
                }

                if (request.EmployeeCode != null) staffProfile.EmployeeCode = request.EmployeeCode;
                if (request.Notes != null) staffProfile.Notes = request.Notes;

                _unitOfWork.StaffProfile.Update(staffProfile);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = false
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Admin update account loi: ");
                throw;
            }
        }
        private static string GenerateEmployeeCode(string role)
           => $"{role}{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        public async Task <ServiceResult<bool>> ActiveAccountAsync(string userID)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userID);
               if(user == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "không tìm thấy user",
                    Data = false
                };
            }

            user.UserStatus = UserStatus.Active;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);
            await _unitOfWork.CommitAsync();
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "kích hoạt thành công",
                Data = true
            };
        }
    }
}
