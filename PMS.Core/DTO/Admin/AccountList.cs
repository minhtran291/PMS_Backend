using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Admin
{
    public class AccountList
    {
        public string UserId { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public UserStatus UserStatus { get; set; }
        public DateTime CreateAt { get; set; }

        public string? FullName { get; set; }
        public Gender Gender { get; set; }
        public string? Department { get; set; }
        public bool IsStaff => !string.IsNullOrEmpty(Department);
        public bool IsCustomer { get; set; }
    }
}
