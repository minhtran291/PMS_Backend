using PMS.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Admin
{
    public class AccountList
    {
        public string UserId { get; set; } = null!;
        [Required(ErrorMessage = "Email là bắt buộc!")]
        public string? Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public UserStatus UserStatus { get; set; }
        public DateTime CreateAt { get; set; }
        public string? Role { get; set; }
        public string? Address { get; set; }
        public string? FullName { get; set; }
        public bool? Gender { get; set; }
        public string? EmployeeCode { get; set; }
        public bool IsStaff { get; set; }
        public bool IsCustomer { get; set; }
    }
}
