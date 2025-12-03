using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Auth;
using PMS.Application.DTOs.Customer;
using PMS.Application.DTOs.Profile;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;

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
        Task<ServiceResult<bool>> UpdateCustomerProfile(string userId, PMS.Application.DTOs.Customer.CustomerProfileDTO request);
        Task<ServiceResult<IEnumerable<CustomerDTO>>> GetAllCustomerWithInactiveStatus();
        Task<ServiceResult<bool>> UpdateCustomerStatus(string userId, string verifier, UserStatus status, string note);
        Task<ServiceResult<CustomerViewDTO>> GetCustomerByIdAsync(string userId);
        Task<ServiceResult<bool>> ChangePasswordAsync(string userId, string oldPasword, string newpassword);
        Task<ServiceResult<object>> GetProfile(string userId, List<string> roles);
        Task<ServiceResult<CustomerStatusDTO>> GetCustomerStatusAsync(string userId);
        Task<ServiceResult<bool>> SubmitCustomerAdditionalInfoAsync(string userId, CustomerAdditionalInfoDTO additionalInfo);
        Task<FileContentResult?> DownloadCustomerImageAsync(string userId, string type);
        Task<bool> EditProfileAsync(
        string userId,
        CustomerEditProfileDTO dto,
        IFormFile? avatarFile,
        IFormFile? cnkdFile,
        IFormFile? bytFile);


    }
}
