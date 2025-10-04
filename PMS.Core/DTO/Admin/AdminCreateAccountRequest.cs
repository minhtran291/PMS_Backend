using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Admin
{
    public class AdminCreateAccountRequest
    {
        // User (Identity)
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;   

        // Profile
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public Gender Gender { get; set; }
        public string? Address { get; set; }

        // StaffProfile
        public string? EmployeeCode { get; set; }    
        public string? Department { get; set; }
        public string? Notes { get; set; }
    }
}
