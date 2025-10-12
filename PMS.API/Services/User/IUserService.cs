using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Content;
using PMS.Core.DTO.Request;

namespace PMS.API.Services.User
{
    public interface IUserService
    {
        Task<Core.Domain.Identity.User?> GetUserById(string userId);
        Task<IList<string>> GetUserRoles(Core.Domain.Identity.User user);
        Task<ServiceResult<bool>> RegisterUserAsync(RegisterUser customer);
        Task SendEmailConfirmAsync(Core.Domain.Identity.User user);
        Task<ServiceResult<bool>> ReSendEmailConfirmAsync(ResendConfirmEmailRequest request);
        Task<ServiceResult<bool>> ConfirmEmailAsync(string userId, string token);
        Task<ServiceResult<bool>> SendEmailResetPasswordAsync(string email);
        Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
