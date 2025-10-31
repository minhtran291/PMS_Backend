using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesQuotaionDetails
    {
        public int Id { get; set; }
        public int SqId { get; set; }
        public int? LotId { get; set; } // co the null khi het hang
        public int? TaxId { get; set; }
        public int ProductId { get; set; }
        public string? Note { get; set; }

        public virtual SalesQuotation SalesQuotation { get; set; } = null!;
        public virtual LotProduct? LotProduct { get; set; }
        public virtual TaxPolicy? TaxPolicy { get; set; }
        public virtual Product Product { get; set;} = null!;
    }
}
