using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Profile
{
    public class CustomerEditProfileDTO
    {
        public long? MST { get; set; }
        public long? Mshkd { get; set; }
        public string? FullName { get; set; }
        public string Address { get; set; } = null!;
    }
}
