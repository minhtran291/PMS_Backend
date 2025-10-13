using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Admin
{
    public class AdminUpdateAccountRequest
    {

        // Update User 
        public string? PhoneNumber { get; set; }
        public UserStatus? UserStatus { get; set; }

        // Update Profile
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public Gender? Gender { get; set; }
        public string? Address { get; set; }

        // Update StaffProfile
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Notes { get; set; }
    }
}
