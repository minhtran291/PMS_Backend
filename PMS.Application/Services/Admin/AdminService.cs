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
                    StatusCode = 400,
                    Message = "Email đã được sử dụng",
                    Data = false
                };

            var validatePhone = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (validatePhone != null)
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Message = "Số điện thoại đã được sử dụng",
                    Data = false
                };

            // Validate mã nhân viên không trùng
            var employeeCode = string.IsNullOrWhiteSpace(request.EmployeeCode)
                ? GenerateEmployeeCode(request.StaffRole switch
                {
                    StaffRole.SalesStaff => UserRoles.SALES_STAFF,
                    StaffRole.PurchasesStaff => UserRoles.PURCHASES_STAFF,
                    StaffRole.WarehouseStaff => UserRoles.WAREHOUSE_STAFF,
                    StaffRole.Accountant => UserRoles.ACCOUNTANT,
                    _ => UserRoles.SALES_STAFF
                })
                : request.EmployeeCode;

            var validateEmployeeCode = await _unitOfWork.StaffProfile.Query()
                .FirstOrDefaultAsync(sp => sp.EmployeeCode == employeeCode);

            if (validateEmployeeCode != null)
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Message = "Mã nhân viên đã được sử dụng",
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
                    EmployeeCode = employeeCode, // Sử dụng mã đã được validate
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
        private static StaffRole? MapToSingleStaffRole(IList<string> roleNames)
        {
            if (roleNames == null || roleNames.Count == 0) return null;

            if (roleNames.Contains(UserRoles.SALES_STAFF)) return StaffRole.SalesStaff;
            if (roleNames.Contains(UserRoles.PURCHASES_STAFF)) return StaffRole.PurchasesStaff;
            if (roleNames.Contains(UserRoles.WAREHOUSE_STAFF)) return StaffRole.WarehouseStaff;
            if (roleNames.Contains(UserRoles.ACCOUNTANT)) return StaffRole.Accountant;

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

            var roleOfUser = new Dictionary<string, StaffRole>();
            var roleNamesOfUser = new Dictionary<string, IList<string>>(); // Thêm dictionary để lưu role names
            foreach (var u in result)
            {
                var roleNames = await _unitOfWork.Users.UserManager.GetRolesAsync(u); // IList<string>
                roleOfUser[u.Id] = ToStaffRole(roleNames); // map tên role -> enum StaffRole
                roleNamesOfUser[u.Id] = roleNames; // Lưu role names để sử dụng sau
            }

            return result.Select(u => {
                var roleNames = roleNamesOfUser.TryGetValue(u.Id, out var roles) ? roles : new List<string>();
                var roleName = GetRoleNameFromRoles(roleNames);
                
                // Chỉ set Role cho staff, không set cho admin/manager/customer
                StaffRole role = StaffRole.SalesStaff; // Default fallback
                if (roleNames.Contains(UserRoles.SALES_STAFF) || 
                    roleNames.Contains(UserRoles.PURCHASES_STAFF) || 
                    roleNames.Contains(UserRoles.WAREHOUSE_STAFF) || 
                    roleNames.Contains(UserRoles.ACCOUNTANT))
                {
                    role = roleOfUser.TryGetValue(u.Id, out var r) ? r : StaffRole.SalesStaff;
                }
                
                return new AccountList
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
                    Role = role,
                    RoleName = roleName,
                    IsCustomer = u.CustomerProfile != null,
                    EmailConfirmed = u.EmailConfirmed
                };
            }).ToList();
        }

        private static StaffRole ToStaffRole(IList<string> names)
        {
            // Chỉ map các role staff vào enum StaffRole
            if (names.Contains(UserRoles.SALES_STAFF)) return StaffRole.SalesStaff;
            if (names.Contains(UserRoles.PURCHASES_STAFF)) return StaffRole.PurchasesStaff;
            if (names.Contains(UserRoles.WAREHOUSE_STAFF)) return StaffRole.WarehouseStaff;
            if (names.Contains(UserRoles.ACCOUNTANT)) return StaffRole.Accountant;
            
            // ADMIN, MANAGER, CUSTOMER không map vào StaffRole enum
            // Frontend sẽ sử dụng RoleName để hiển thị đúng vai trò
            return StaffRole.SalesStaff; // Default fallback cho staff
        }

        private static string GetRoleNameFromRoles(IList<string> names)
        {
            // Debug logging
            Console.WriteLine($"GetRoleNameFromRoles - Input roles: [{string.Join(", ", names)}]");
            
            if (names.Contains(UserRoles.ADMIN)) 
            {
                Console.WriteLine("Found ADMIN role, returning 'admin'");
                return "admin";
            }
            if (names.Contains(UserRoles.MANAGER)) 
            {
                Console.WriteLine("Found MANAGER role, returning 'manager'");
                return "manager";
            }
            if (names.Contains(UserRoles.CUSTOMER)) 
            {
                Console.WriteLine("Found CUSTOMER role, returning 'customer'");
                return "customer";
            }
            if (names.Contains(UserRoles.SALES_STAFF)) 
            {
                Console.WriteLine("Found SALES_STAFF role, returning 'sales_staff'");
                return "sales_staff";
            }
            if (names.Contains(UserRoles.PURCHASES_STAFF)) 
            {
                Console.WriteLine("Found PURCHASES_STAFF role, returning 'purchases_staff'");
                return "purchases_staff";
            }
            if (names.Contains(UserRoles.WAREHOUSE_STAFF)) 
            {
                Console.WriteLine("Found WAREHOUSE_STAFF role, returning 'warehouse_staff'");
                return "warehouse_staff";
            }
            if (names.Contains(UserRoles.ACCOUNTANT)) 
            {
                Console.WriteLine("Found ACCOUNTANT role, returning 'accountant_staff'");
                return "accountant_staff";
            }
            
            Console.WriteLine("No matching role found, returning 'unknown'");
            return "unknown";
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

                // Validate email không trùng (nếu có thay đổi)
                if (!string.IsNullOrWhiteSpace(request.Email) && user.Email != request.Email)
                {
                    var validateEmail = await _unitOfWork.Users.UserManager.FindByEmailAsync(request.Email);
                    if (validateEmail != null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ServiceResult<bool>
                        {
                            StatusCode = 400,
                            Message = "Email đã được sử dụng",
                            Data = false
                        };
                    }
                }

                // Validate số điện thoại không trùng (nếu có thay đổi)
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && user.PhoneNumber != request.PhoneNumber)
                {
                    var validatePhone = await _unitOfWork.Users.Query()
                        .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != user.Id);
                    if (validatePhone != null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ServiceResult<bool>
                        {
                            StatusCode = 400,
                            Message = "Số điện thoại đã được sử dụng",
                            Data = false
                        };
                    }
                }

                // Validate mã nhân viên không trùng (nếu có thay đổi)
                if (!string.IsNullOrWhiteSpace(request.EmployeeCode) && 
                    user.StaffProfile?.EmployeeCode != request.EmployeeCode)
                {
                    var validateEmployeeCode = await _unitOfWork.StaffProfile.Query()
                        .FirstOrDefaultAsync(sp => sp.EmployeeCode == request.EmployeeCode && sp.Id != user.StaffProfile!.Id);

                    if (validateEmployeeCode != null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ServiceResult<bool>
                        {
                            StatusCode = 400,
                            Message = "Mã nhân viên đã được sử dụng",
                            Data = false
                        };
                    }
                }

                // Update User
                if (!string.IsNullOrWhiteSpace(request.Email))
                    user.Email = request.Email;

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
        private static string GenerateEmployeeCode(string role )
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
