using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Content;
using PMS.Core.DTO.Request;

namespace PMS.API.Services.User
{
    public interface IUserService
    {
        Task<Core.Domain.Identity.User?> GetUserById(string userId);
        Task<IList<string>> GetUserRoles(Core.Domain.Identity.User user);
        Task RegisterUserAsync(RegisterUser customer);
        Task SendEmailConfirmAsync(Core.Domain.Identity.User user);
        Task ReSendEmailConfirmAsync(ResendConfirmEmailRequest request);
        Task ConfirmEmailAsync(string userId, string token);
        Task SendEmailResetPasswordAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequest request);
    }
}
