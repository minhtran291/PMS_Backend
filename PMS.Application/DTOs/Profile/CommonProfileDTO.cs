using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Profile
{
    public class CommonProfileDTO
    {
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? Avatar { get; set; }
        public string? FullName { get; set; }
        public bool? Gender { get; set; }
    }
}
