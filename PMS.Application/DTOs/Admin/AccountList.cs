using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Admin
{
    public class AccountList
    {
        public string UserId { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public UserStatus UserStatus { get; set; }
        public DateTime CreateAt { get; set; }

        public string? FullName { get; set; }
        public bool? Gender { get; set; }
        public string? Department { get; set; }
        public bool IsStaff => !string.IsNullOrEmpty(Department);
        public bool IsCustomer { get; set; }
    }
}
