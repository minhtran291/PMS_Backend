using AutoMapper;
using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.DTO.Admin;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Admin;
using DProfile = PMS.Core.Domain.Entities.Profile;
using DStaffProfile = PMS.Core.Domain.Entities.StaffProfile;
using DUser = PMS.Core.Domain.Identity.User;

namespace PMS.API.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly PMSContext _context;
        private readonly IAdminRepository _repo;
        private readonly UserManager<DUser> _userManager;
        private readonly IMapper _mapper;

        public AdminService(PMSContext context, 
            IAdminRepository repo,
            UserManager<DUser> userManager,
            IMapper mapper)
        {
            _context = context;
            _repo = repo;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<string> CreateAccountAsync(AdminCreateAccountRequest request, CancellationToken ct = default)
        {
            // Tạo User bằng Identity (để hashing password & validate)
            var existed = await _repo.GetUserByEmailAsync(request.Email, ct);
            if (existed != null)
                throw new InvalidOperationException("Email đã tồn tại.");

            var user = new DUser
            {
                UserName = request.Email,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                UserStatus = UserStatus.Active,
                CreateAt = DateTime.UtcNow
            };

            using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var msg = string.Join("; ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Không thể tạo tài khoản: {msg}");
                }

                // Tạo Profile
                var profile = new DProfile
                {
                    UserId = user.Id,
                    FullName = request.FullName,
                    Avatar = request.Avatar,
                    Gender = request.Gender,
                    Address = request.Address
                };
                await _repo.AddProfileAsync(profile, ct);
                await _repo.SaveChangesAsync(ct);

                // Tạo StaffProfile
                var staff = new StaffProfile
                {
                    ProfileId = profile.Id,
                    EmployeeCode = string.IsNullOrWhiteSpace(request.EmployeeCode)
                        ? GenerateEmployeeCode()
                        : request.EmployeeCode,
                    Department = request.Department,
                    Notes = request.Notes
                };
                await _repo.AddStaffProfileAsync(staff, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return user.Id;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<AdminAccountDetail?> GetAccountDetailAsync(string userId, CancellationToken ct = default)
        {
            var u = await _repo.GetUserWithProfilesAsync(userId, ct);
            if (u == null) return null;

            return new AdminAccountDetail
            {
                UserId = u.Id,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                UserStatus = u.UserStatus,
                CreateAt = u.CreateAt,

                ProfileId = u.Profile.Id,
                FullName = u.Profile.FullName,
                Avatar = u.Profile.Avatar,
                Gender = u.Profile.Gender,
                Address = u.Profile.Address,

                StaffProfileId = u.Profile.StaffProfile?.Id,
                EmployeeCode = u.Profile.StaffProfile?.EmployeeCode,
                Department = u.Profile.StaffProfile?.Department,
                Notes = u.Profile.StaffProfile?.Notes,

                CustomerProfileId = u.Profile.CustomerProfile?.Id,
                Mst = u.Profile.CustomerProfile?.Mst,
                ImageCnkd = u.Profile.CustomerProfile?.ImageCnkd,
                ImageByt = u.Profile.CustomerProfile?.ImageByt,
                Mshkd = u.Profile.CustomerProfile?.Mshkd
            };
        }

        public async Task<List<AdminAccountListItem>> GetAccountsAsync(string? keyword, CancellationToken ct = default)
        {
            var users = await _repo.GetUsersAsync(keyword, ct);
            return users.Select(u => new AdminAccountListItem
            {
                UserId = u.Id,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                UserStatus = u.UserStatus,
                CreateAt = u.CreateAt,
                FullName = u.Profile?.FullName,
                Gender = u.Profile?.Gender ?? Gender.Other,
                Department = u.Profile?.StaffProfile?.Department,
                IsCustomer = u.Profile?.CustomerProfile != null
            }).ToList();
        }

        public async Task SuspendAccountAsync(string userId, CancellationToken ct = default)
        {
            var u = await _repo.GetUserWithProfilesAsync(userId, ct)
                ?? throw new KeyNotFoundException("User không tồn tại.");

            u.UserStatus = UserStatus.Block;
            await _repo.SaveChangesAsync(ct);
        }

        public async Task UpdateAccountAsync(string userId, AdminUpdateAccountRequest request, CancellationToken ct = default)
        {
            var u = await _repo.GetUserWithProfilesAsync(userId, ct)
                ?? throw new KeyNotFoundException("User không tồn tại.");

            using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                // Update User
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    u.PhoneNumber = request.PhoneNumber;
                if (request.UserStatus.HasValue)
                    u.UserStatus = request.UserStatus.Value;

                // Update Profile
                var p = u.Profile;
                if (p == null)
                    throw new InvalidOperationException("User chưa có Profile.");

                if (request.FullName != null) p.FullName = request.FullName;
                if (request.Avatar != null) p.Avatar = request.Avatar;
                if (request.Gender.HasValue) p.Gender = request.Gender.Value;
                if (request.Address != null) p.Address = request.Address;

                await _repo.UpdateProfileAsync(p);

                // Update / Upsert StaffProfile
                if (u.Profile.StaffProfile == null)
                {
                    // nếu admin muốn thêm staff profile mới
                    if (request.EmployeeCode != null || request.Department != null || request.Notes != null)
                    {
                        var sp = new StaffProfile
                        {
                            ProfileId = p.Id,
                            EmployeeCode = string.IsNullOrWhiteSpace(request.EmployeeCode)
                                ? GenerateEmployeeCode()
                                : request.EmployeeCode,
                            Department = request.Department,
                            Notes = request.Notes
                        };
                        await _repo.AddStaffProfileAsync(sp, ct);
                    }
                }
                else
                {
                    var sp = u.Profile.StaffProfile;
                    if (request.EmployeeCode != null) sp.EmployeeCode = request.EmployeeCode;
                    if (request.Department != null) sp.Department = request.Department;
                    if (request.Notes != null) sp.Notes = request.Notes;

                    await _repo.UpdateStaffProfileAsync(sp);
                }

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        private static string GenerateEmployeeCode()
           => $"EMP{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}
