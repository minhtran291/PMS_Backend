using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesQuotation
    {
        public int Id { get; set; }
        public int RsqId { get; set; }
        public string QuotationCode { get; set; } = null!;
        public DateTime QuotationDate {  get; set; }
        public DateTime ExpiredDate { get; set; }
        public SalesQuotationStatus Status { get; set; }
        public string? Notes {  get; set; }
    }
}
