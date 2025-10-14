using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class RequestSalesQuotationDetails
    {
        public int RequestSalesQuotationId { get; set; }
        public int ProductId { get; set; }

        public virtual RequestSalesQuotation RequestSalesQuotation { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
