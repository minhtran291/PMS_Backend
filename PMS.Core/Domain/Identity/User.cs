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
        public DateTime? RefreshTokenExpiriryTime { get; set; }
        public UserStatus UserStatus { get; set; }
        public virtual Profile Profile { get; set; } = null!;
        public DateTime CreateAt { get; set; }
    }
}
