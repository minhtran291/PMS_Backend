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
        public string UserId { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual ICollection<SalesQuotation>? SalesQuotations { get; set; }
    }
}
