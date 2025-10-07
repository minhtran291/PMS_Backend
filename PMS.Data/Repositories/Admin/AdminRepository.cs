using Microsoft.EntityFrameworkCore;
using PMS.Data.DatabaseConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DProfile = PMS.Core.Domain.Entities.Profile;
using DStaffProfile = PMS.Core.Domain.Entities.StaffProfile;
using DUser = PMS.Core.Domain.Identity.User;

namespace PMS.Data.Repositories.Admin
{
    public class AdminRepository : IAdminRepository
    {
        private readonly PMSContext _context;

        public AdminRepository(PMSContext context) => _context = context;

        public async Task AddProfileAsync(DProfile profile, CancellationToken ct = default)
        {
            await _context.Profiles.AddAsync(profile, ct);
        }

        public async Task AddStaffProfileAsync(DStaffProfile staff, CancellationToken ct = default)
        {
            await _context.StaffProfiles.AddAsync(staff, ct);
        }

        public async Task<DUser?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _context.Users
                .Include(u => u.Profile).ThenInclude(p => p.StaffProfile)
                .Include(u => u.Profile).ThenInclude(p => p.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        public async Task<List<DUser>> GetUsersAsync(string? keyword, CancellationToken ct = default)
        {
            var q = QueryUsers();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                q = q.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(k)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(k)) ||
                    (u.Profile.FullName != null && u.Profile.FullName.ToLower().Contains(k)) ||
                    (u.Profile.StaffProfile != null &&
                     u.Profile.StaffProfile.Department != null &&
                     u.Profile.StaffProfile.Department.ToLower().Contains(k)));
            }

            return await q.OrderByDescending(u => u.CreateAt).ToListAsync(ct);
        }

        public async Task<DUser?> GetUserWithProfilesAsync(string userId, CancellationToken ct = default)
        {
            var q = _context.Users
        .Include(u => u.Profile).ThenInclude(p => p.StaffProfile)
        .Include(u => u.Profile).ThenInclude(p => p.CustomerProfile);

            if (int.TryParse(userId, out var profileId))
                return await q.FirstOrDefaultAsync(u => u.Profile.Id == profileId, ct);

            return await q.FirstOrDefaultAsync(u => u.Id == userId, ct);
        }

        public IQueryable<DUser> QueryUsers()
        {
            return _context.Users
                .Include(u => u.Profile).ThenInclude(p => p.StaffProfile)
                .Include(u => u.Profile).ThenInclude(p => p.CustomerProfile)
                .AsQueryable();
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        public Task UpdateProfileAsync(DProfile profile)
        {
            _context.Profiles.Update(profile);
            return Task.CompletedTask;
        }

        public Task UpdateStaffProfileAsync(DStaffProfile staff)
        {
            _context.StaffProfiles.Update(staff);
            return Task.CompletedTask;
        }
    }
}
