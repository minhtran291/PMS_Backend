using PMS.Core.DTO.Admin;
using DProfile = PMS.Core.Domain.Entities.Profile;
using DStaffProfile = PMS.Core.Domain.Entities.StaffProfile;
using DUser = PMS.Core.Domain.Identity.User;

namespace PMS.API.Services.Admin
{
    public interface IAdminService
    {
        Task<string> CreateAccountAsync(AdminCreateAccountRequest request, CancellationToken ct = default);
        Task<List<AdminAccountListItem>> GetAccountsAsync(string? keyword, CancellationToken ct = default);
        Task<AdminAccountDetail?> GetAccountDetailAsync(string userId, CancellationToken ct = default);
        Task UpdateAccountAsync(AdminUpdateAccountRequest request, CancellationToken ct = default);
        Task SuspendAccountAsync(string userId, CancellationToken ct = default);
    }
}
