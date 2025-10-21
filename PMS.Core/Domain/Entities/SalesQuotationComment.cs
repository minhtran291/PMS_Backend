using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesQuotationComment
    {
        public int Id { get; set; }
        public int SqId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Content { get; set; }

        public virtual SalesQuotation SalesQuotation { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
