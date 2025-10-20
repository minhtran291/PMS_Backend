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
        public string UserId { get; set; } = null!;
        public string? Content { get; set; }
    }
}
