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
        public int SqvId { get; set; }
        public int SsId { get; set; }
        public string QuotationCode { get; set; } = string.Empty;
        public DateTime? QuotationDate { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public SalesQuotationStatus Status { get; set; }
        public string? Notes { get; set; }

        public virtual RequestSalesQuotation RequestSalesQuotation { get; set; } = null!;
        public virtual SalesQuotationValidity SalesQuotationValidity { get; set; } = null!;
        public virtual ICollection<SalesQuotaionDetails> SalesQuotaionDetails { get; set; } = [];
        public virtual ICollection<SalesQuotationComment>? SalesQuotationComments { get; set; }
        public virtual StaffProfile StaffProfile { get; set; } = null!;
    }
}
