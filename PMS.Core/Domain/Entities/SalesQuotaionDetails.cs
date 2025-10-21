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
        public int LotId { get; set; }
        public int TaxId { get; set; }

        public virtual SalesQuotation SalesQuotation { get; set; } = null!;
        public virtual LotProduct LotProduct { get; set; } = null!;
        public virtual TaxPolicy TaxPolicy { get; set; } = null!;
    }
}
