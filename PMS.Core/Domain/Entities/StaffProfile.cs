using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class StaffProfile
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Notes { get; set; }
        public virtual Profile Profile { get; set; } = null!;
    }
}
