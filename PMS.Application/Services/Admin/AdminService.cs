using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Admin;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.DatabaseConfig;
using PMS.Data.UnitOfWork;

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
                    _logger.LogError("Tạo người dùng thất bại: {Errors}", errors);
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "Mật khẩu phải từ 8 kí tự và phải chứ ít nhất một kí tự đặc biệt, một chữ cái thường và một chữ cái in hoa.",
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
                    _logger.LogError("Gán vai trò thất bại: {Errors}", errors);
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "Không gán được vai trò cho nhân viên.",
                        Data = false
                    };
                }

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Tạo thành công tài khoản nhân viên",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Tạo nhân viên thất bại");
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
            var roleNames = await _unitOfWork.Users.UserManager.GetRolesAsync(user);
            var staffRole = MapToSingleStaffRole(roleNames);

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
                StaffRole = staffRole,
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

        //Get role to account details 
        private static string MapToSingleStaffRole(IList<string> roleNames)
        {
            if (roleNames == null || roleNames.Count == 0)
                return null; // hoặc default bạn muốn

            if (roleNames.Contains(UserRoles.SALES_STAFF)) return UserRoles.SALES_STAFF;
            if (roleNames.Contains(UserRoles.PURCHASES_STAFF)) return UserRoles.PURCHASES_STAFF;
            if (roleNames.Contains(UserRoles.WAREHOUSE_STAFF)) return UserRoles.WAREHOUSE_STAFF;
            if (roleNames.Contains(UserRoles.ACCOUNTANT)) return UserRoles.ACCOUNTANT;
            if (roleNames.Contains(UserRoles.CUSTOMER)) return UserRoles.CUSTOMER;
            if (roleNames.Contains(UserRoles.ADMIN)) return UserRoles.ADMIN;
            if (roleNames.Contains(UserRoles.MANAGER)) return UserRoles.MANAGER;

            return null;
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

            var roleOfUser = new Dictionary<string, string>();
            foreach (var u in result)
            {
                var roleNames = await _unitOfWork.Users.UserManager.GetRolesAsync(u);
                roleOfUser[u.Id] = ToStaffRole(roleNames);
            }

            return result.Select(u => new AccountList
            {
                UserId = u.Id,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                UserStatus = u.UserStatus,
                CreateAt = u.CreateAt,
                FullName = u.FullName,
                Address = u.Address,
                Gender = u.Gender,
                EmployeeCode = u.StaffProfile?.EmployeeCode,
                Role = roleOfUser.TryGetValue(u.Id, out var r) ? r : null,
                IsCustomer = u.CustomerProfile != null
            }).ToList();
        }

        //Get role to account list
        private static string? ToStaffRole(IList<string> roleNames)
        {
            if (roleNames == null || roleNames.Count == 0)
                return null; // hoặc default bạn muốn

            if (roleNames.Contains(UserRoles.SALES_STAFF)) return UserRoles.SALES_STAFF;
            if (roleNames.Contains(UserRoles.PURCHASES_STAFF)) return UserRoles.PURCHASES_STAFF;
            if (roleNames.Contains(UserRoles.WAREHOUSE_STAFF)) return UserRoles.WAREHOUSE_STAFF;
            if (roleNames.Contains(UserRoles.ACCOUNTANT)) return UserRoles.ACCOUNTANT;
            if (roleNames.Contains(UserRoles.CUSTOMER)) return UserRoles.CUSTOMER;
            if (roleNames.Contains(UserRoles.ADMIN)) return UserRoles.ADMIN;
            if (roleNames.Contains(UserRoles.MANAGER)) return UserRoles.MANAGER;

            return null;
        }

        //Change account status to Inactive
        public async Task<ServiceResult<bool>> SuspendAccountAsync(string userId)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "không tìm thấy người dùng",
                    Data = false
                };
            }

            user.UserStatus = UserStatus.Block;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "Ngừng hoạt động tài khoản thành công",
                Data = true
            };
        }

        //Update account information
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
                    Message = "không tìm thấy người dùng",
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

                // Update StaffProfile
                var staffProfile = user.StaffProfile;
                if (staffProfile == null)
                {
                    _logger.LogWarning("Update profile loi, khong co staff profile");
                    return new ServiceResult<bool>
                    {
                        StatusCode = 500,
                        Message = "Update profile lỗi, không có staff profile",
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
                    Message = "Cập Nhật Thành công",
                    Data = false
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Có lỗi trong khi cập nhật thông tin tài khoản: ");
                throw;
            }
        }

        //Generate EmployeeCode acording to roles
        private static string GenerateEmployeeCode(string role )
           => $"{role}{DateTime.Now:yyyyMMddHHmm}";

        //Change account status to Active
        public async Task <ServiceResult<bool>> ActiveAccountAsync(string userID)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userID);
               if(user == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Message = "không tìm thấy người dùng",
                    Data = false
                };
            }

            user.UserStatus = UserStatus.Active;

            await _unitOfWork.Users.UserManager.UpdateAsync(user);
            await _unitOfWork.CommitAsync();
            return new ServiceResult<bool>
            {
                StatusCode = 200,
                Message = "kích hoạt tài khoản thành công",
                Data = true
            };
        }
    }
}
