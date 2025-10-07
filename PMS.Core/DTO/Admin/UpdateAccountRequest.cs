using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Admin
{
    public class UpdateAccountRequest
    {
        // Update User 
        public string UserId {  get; set; }
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
