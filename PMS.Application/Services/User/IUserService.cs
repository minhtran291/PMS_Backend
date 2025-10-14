using PMS.Application.DTOs.Auth;
using PMS.Application.DTOs.Customer;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.User
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
        Task<ServiceResult<bool>> UpdateCustomerProfile(string userId, CustomerProfileDTO request);
        Task<ServiceResult<IEnumerable<CustomerDTO>>> GetAllCustomerWithInactiveStatus();
        Task<ServiceResult<bool>> UpdateCustomerStatus(string userId, string managerId);
        Task<ServiceResult<CustomerViewDTO>> GetCustomerByIdAsync(string userId);
    }
}
