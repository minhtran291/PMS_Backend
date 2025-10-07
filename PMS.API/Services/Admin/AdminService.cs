using AutoMapper;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.DTO.Admin;
using PMS.API.Services.Base;
using PMS.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using PMS.Core.Domain.Constant;

namespace PMS.API.Services.Admin
{
    public class AdminService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AdminService> logger) : Service(unitOfWork, mapper), IAdminService
    {
        private readonly ILogger<AdminService> _logger = logger;

        public async Task CreateAccountAsync(CreateAccountRequest request)
        {
            var validateEmail = await _unitOfWork.Users.UserManager.FindByEmailAsync(request.Email);

            if (validateEmail != null)
                throw new Exception("Email đã được sử dụng");

            var validatePhone = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (validatePhone != null)
                throw new Exception("Số điện thoại đã được sử dụng");

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
                    EmailConfirmed = true
                };

                var createResult = await _unitOfWork.Users.UserManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("Tao nguoi dung that bai: {Errors}", errors);
                    throw new Exception("Có lỗi xảy ra");
                }

                // Tạo Profile
                var profile = new Core.Domain.Entities.Profile
                {
                    UserId = user.Id,
                    FullName = request.FullName,
                    Address = request.Address,
                    Avatar = "https://as2.ftcdn.net/v2/jpg/03/31/69/91/1000_F_331699188_lRpvqxO5QRtwOM05gR50ImaaJgBx68vi.jpg",
                    Gender = request.Gender
                };
                await _unitOfWork.Profile.AddAsync(profile);
                await _unitOfWork.CommitAsync();

                // Tạo StaffProfile
                var staffProfile = new StaffProfile
                {
                    ProfileId = profile.Id,
                    EmployeeCode = string.IsNullOrWhiteSpace(request.EmployeeCode)
                        ? GenerateEmployeeCode()
                        : request.EmployeeCode,
                    Department = request.Department,
                    Notes = request.Notes
                };
                await _unitOfWork.StaffProfile.AddAsync(staffProfile);

                var role = request.StaffRole switch
                {
                    StaffRole.SalesStaff => UserRoles.SALES_STAFF,
                    StaffRole.PurchasesStaff => UserRoles.PURCHASES_STAFF,
                    StaffRole.WarehouseStaff => UserRoles.WAREHOUSE_STAFF,
                    _ => throw new Exception("Role không hợp lệ")
                };

                var roleResult = await _unitOfWork.Users.UserManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogError("Gan role that bai: {Errors}", errors);
                    throw new Exception("Có lỗi xảy ra");
                }

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Tao nhan vien that bai");
                throw;
            }
        }

        public async Task<AccountDetails> GetAccountDetailAsync(string userId)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(u => u.Profile)
                    .ThenInclude(p => p.StaffProfile)
                .Include(u => u.Profile)
                    .ThenInclude(p => p.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId)
                    ?? throw new Exception("Không tìm thấy người dùng");

            var roles = await _unitOfWork.Users.UserManager.GetRolesAsync(user);

            return new AccountDetails
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                UserStatus = user.UserStatus,
                CreateAt = user.CreateAt,

                ProfileId = user.Profile.Id,
                FullName = user.Profile.FullName,
                Avatar = user.Profile.Avatar,
                Gender = user.Profile.Gender,
                Address = user.Profile.Address,

                StaffProfileId = user.Profile.StaffProfile?.Id,
                EmployeeCode = user.Profile.StaffProfile?.EmployeeCode,
                Department = user.Profile.StaffProfile?.Department,
                Notes = user.Profile.StaffProfile?.Notes,

                CustomerProfileId = user.Profile.CustomerProfile?.Id,
                Mst = user.Profile.CustomerProfile?.Mst,
                ImageCnkd = user.Profile.CustomerProfile?.ImageCnkd,
                ImageByt = user.Profile.CustomerProfile?.ImageByt,
                Mshkd = user.Profile.CustomerProfile?.Mshkd
            };
        }

        public async Task<List<AccountList>> GetAccountListAsync(string? keyword)
        {
            var users = _unitOfWork.Users.Query()
                    .Include(u => u.Profile).ThenInclude(p => p.StaffProfile)
                    .Include(u => u.Profile).ThenInclude(p => p.CustomerProfile)
                    .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                var handleKeyword = keyword.Trim().ToLower();

                users = users.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(handleKeyword)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(handleKeyword)) ||
                    (u.Profile.FullName != null && u.Profile.FullName.ToLower().Contains(handleKeyword)) ||
                    (u.Profile.StaffProfile != null &&
                     u.Profile.StaffProfile.Department != null &&
                     u.Profile.StaffProfile.Department.ToLower().Contains(handleKeyword)));
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
                FullName = u.Profile?.FullName,
                Gender = u.Profile?.Gender ?? Gender.Other,
                Department = u.Profile?.StaffProfile?.Department,
                IsCustomer = u.Profile?.CustomerProfile != null
            }).ToList();
        }

        public async Task SuspendAccountAsync(string userId)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                ?? throw new Exception("User không tồn tại.");

            user.UserStatus = UserStatus.Block;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }

        public async Task UpdateAccountAsync(UpdateAccountRequest request)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(u => u.Profile)
                    .ThenInclude(u => u.StaffProfile)
                .FirstOrDefaultAsync(u => u.Id == request.UserId)
                    ?? throw new Exception("Người dùng không tồn tại.");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Update User
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    user.PhoneNumber = request.PhoneNumber;

                if (request.UserStatus.HasValue)
                    user.UserStatus = request.UserStatus.Value;

                // Update Profile
                var profile = user.Profile;
                if (profile == null)
                {
                    _logger.LogWarning("Update profile loi, user chua co profile");
                    throw new Exception("Có lỗi xảy ra");
                }

                if (request.FullName != null) profile.FullName = request.FullName;
                if (request.Avatar != null) profile.Avatar = request.Avatar;
                if (request.Gender.HasValue) profile.Gender = request.Gender.Value;
                if (request.Address != null) profile.Address = request.Address;

                _unitOfWork.Profile.Update(profile);

                // Update / Upsert StaffProfile
                var staffProfile = user.Profile.StaffProfile;
                if (staffProfile == null)
                {
                    _logger.LogWarning("Update profile loi, khong co staff profile");
                    throw new Exception("Có lỗi xảy ra");
                }

                if (request.EmployeeCode != null) staffProfile.EmployeeCode = request.EmployeeCode;
                if (request.Department != null) staffProfile.Department = request.Department;
                if (request.Notes != null) staffProfile.Notes = request.Notes;

                _unitOfWork.StaffProfile.Update(staffProfile);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Admin update account loi: ");
                throw;
            }
        }
        private static string GenerateEmployeeCode()
           => $"EMP{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}
