using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DUser = PMS.Core.Domain.Identity.User;
using DProfile = PMS.Core.Domain.Entities.Profile;
using DStaffProfile = PMS.Core.Domain.Entities.StaffProfile;


namespace PMS.Data.Repositories.Admin
{
    public interface IAdminRepository
    {
        Task<DUser?> GetUserWithProfilesAsync(string userId, CancellationToken ct = default);
        Task<DUser?> GetUserByEmailAsync(string email, CancellationToken ct = default);
        Task<List<DUser>> GetUsersAsync(string? keyword, CancellationToken ct = default);

        Task AddProfileAsync(DProfile profile, CancellationToken ct = default);
        Task AddStaffProfileAsync(DStaffProfile staff, CancellationToken ct = default);

        Task UpdateProfileAsync(DProfile profile);
        Task UpdateStaffProfileAsync(DStaffProfile staff);

        Task SaveChangesAsync(CancellationToken ct = default);
        IQueryable<DUser> QueryUsers();
    }
}
