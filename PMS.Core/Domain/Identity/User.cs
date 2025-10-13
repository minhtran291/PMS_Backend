using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Identity
{
    public class User : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpriryTime { get; set; }
        public UserStatus UserStatus { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
        public bool? Gender { get; set; }
        public DateTime CreateAt { get; set; }
        
        public virtual SalesStaffProfile? SalesStaffProfile {  get; set; }
        public virtual CustomerProfile? CustomerProfile { get; set; }
    }
}
