using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class RequestSalesQuotation
    {
        public int Id { get; set; }
        public int CustomerId {  get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public DateTime? RequestDate { get; set; }
        public RequestSalesQuotationStatus Status { get; set; }

        public virtual CustomerProfile CustomerProfile { get; set; } = null!;
        public virtual ICollection<RequestSalesQuotationDetails> RequestSalesQuotationDetails { get; set; } = [];
        public virtual ICollection<SalesQuotation>? SalesQuotations { get; set; }
    }
}