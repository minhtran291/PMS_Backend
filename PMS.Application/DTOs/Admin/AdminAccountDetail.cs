using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Admin
{
    public class AdminAccountDetail
    {
        // User
        public string UserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public UserStatus UserStatus { get; set; }
        public DateTime CreateAt { get; set; }

        // Profile
        public int ProfileId { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public Gender Gender { get; set; }
        public string? Address { get; set; }

        // Staff
        public int? StaffProfileId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Notes { get; set; }

        // Customer (view-only)
        public int? CustomerProfileId { get; set; }
        public long? Mst { get; set; }
        public string? ImageCnkd { get; set; }
        public string? ImageByt { get; set; }
        public long? Mshkd { get; set; }
    }
}
