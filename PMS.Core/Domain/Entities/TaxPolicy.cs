using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class TaxPolicy
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "VAT 10%", "Không chịu thuế"
        public decimal Rate { get; set; } // 0.1, 0.05, 0
        public string? Description { get; set; }
        public bool Status { get; set; }

        public virtual ICollection<SalesQuotaionDetails> SalesQuotaionDetails { get; set; } = [];
    }
}
