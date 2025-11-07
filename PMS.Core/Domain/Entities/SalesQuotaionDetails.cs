using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesQuotaionDetails
    {
        public int SqId { get; set; }
        public int ProductId { get; set; }
        public decimal SalesPrice {  get; set; }
        public required string ExpectedExpiryNote { get; set; }
        public int TaxId { get; set; }
        public string? Note { get; set; }

        public virtual SalesQuotation SalesQuotation { get; set; } = null!;
        public virtual TaxPolicy TaxPolicy { get; set; } = null!;
        public virtual Product Product { get; set;} = null!;
    }
}
