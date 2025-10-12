using PMS.Core.Domain.Constant;
using PMS.Core.DTO.Admin;
using DProfile = PMS.Core.Domain.Entities.Profile;
using DStaffProfile = PMS.Core.Domain.Entities.StaffProfile;
using DUser = PMS.Core.Domain.Identity.User;

namespace PMS.API.Services.Admin
{
    public interface IAdminService
    {
        Task<ServiceResult<bool>> CreateAccountAsync(CreateAccountRequest request);
        Task<List<AccountList>> GetAccountListAsync(string? keyword);
        Task<ServiceResult<AccountDetails>> GetAccountDetailAsync(string userId);
        Task<ServiceResult<bool>> UpdateAccountAsync(UpdateAccountRequest request);
        Task<ServiceResult<bool>> SuspendAccountAsync(string userId);
        Task<ServiceResult<bool>> ActiveAccountAsync(string userID);
    }
}
