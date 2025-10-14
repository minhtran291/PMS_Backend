using PMS.Core.Domain.Constant;
using PMS.Application.DTOs.Admin;

namespace PMS.Application.Services.Admin
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
