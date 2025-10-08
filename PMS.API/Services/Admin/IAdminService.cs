using PMS.Core.DTO.Admin;
using DProfile = PMS.Core.Domain.Entities.Profile;
using DStaffProfile = PMS.Core.Domain.Entities.StaffProfile;
using DUser = PMS.Core.Domain.Identity.User;

namespace PMS.API.Services.Admin
{
    public interface IAdminService
    {
        Task CreateAccountAsync(CreateAccountRequest request);
        Task<List<AccountList>> GetAccountListAsync(string? keyword);
        Task<AccountDetails> GetAccountDetailAsync(string userId);
        Task UpdateAccountAsync(UpdateAccountRequest request);
        Task SuspendAccountAsync(string userId);
        Task ActiveAccountAsync(string userID);
    }
}
