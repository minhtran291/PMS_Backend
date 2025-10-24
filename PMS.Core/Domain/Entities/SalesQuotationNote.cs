using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesQuotationNote
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content {  get; set; } = null!;
        public bool IsActive { get; set; }

        public virtual ICollection<SalesQuotation> SalesQuotations { get; set; } = [];
    }
}
