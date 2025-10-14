using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Customer
{
    public class CustomerViewDTO
    {
        // From User
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
        public bool? Gender { get; set; }
        public DateTime CreateAt { get; set; }
        public UserStatus UserStatus { get; set; }

        // From CustomerProfile
        public int CustomerProfileId { get; set; }
        public long? Mst { get; set; }
        public string? ImageCnkd { get; set; }
        public string? ImageByt { get; set; }
        public long? Mshkd { get; set; }
    }
}
