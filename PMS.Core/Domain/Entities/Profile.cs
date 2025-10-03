using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class Profile
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public Gender Gender { get; set; }
        public string? Address {  get; set; }
        public virtual User User { get; set; } = null!;
        public virtual CustomerProfile? CustomerProfile { get; set; }
        public virtual StaffProfile? StaffProfile { get; set;}
    }
}
