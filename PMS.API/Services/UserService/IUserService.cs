using PMS.Core.Domain.Identity;
using PMS.Core.DTO.Content;

namespace PMS.API.Services.UserService
{
    public interface IUserService
    {
        Task RegisterUserAsync(RegisterUser customer);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserAsync(User user);
        Task VerifyJwtTokenAsync(string token);
        Task<bool> InitiatePasswordResetAsync(string email);
        Task ResetPasswordAsync(string token, string newPassword);
    }
}
