using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Admin
{
    public class AdminAccountListItem
    {
        public string UserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public UserStatus UserStatus { get; set; }
        public DateTime CreateAt { get; set; }
        public string? FullName { get; set; }
        public Gender Gender { get; set; }
        public bool IsStaff => UserStatus.Equals(UserStatus.Active);
        public bool IsCustomer { get; set; }  
    }
}
