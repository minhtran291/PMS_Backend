using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Admin
{
    public class CreateAccountRequest
    {
        // User (Identity)
        public string Email { get; set; } = null!;
        public string UserName { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Role là bắt buộc")]
        [EnumDataType(typeof(StaffRole), ErrorMessage = "Role không hợp lệ")]
        public StaffRole StaffRole { get; set; }

        // Profile
        public string? FullName { get; set; }
        public Gender Gender { get; set; }
        public string? Address { get; set; }

        // StaffProfile
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Notes { get; set; }
    }
}
