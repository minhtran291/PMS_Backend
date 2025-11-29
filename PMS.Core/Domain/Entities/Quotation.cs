using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Core.Domain.Entities
{
    public class Quotation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required int QID { get; set; }
        public required DateTime SendDate { get; set; }
        public required int SupplierID { get; set; }
        public SupplierQuotationStatus Status { get; set; }
        public required DateTime QuotationExpiredDate { get; set; }

        public int PaymentDueDate{get; set; }

        public int PRFQID { get; set; }
        [ForeignKey("PRFQID")]
        public virtual PurchasingRequestForQuotation PurchasingRequestForQuotation { get; set; } = null!;

        public virtual ICollection<PurchasingOrder> PurchasingOrders { get; set; } = new List<PurchasingOrder>();
        public virtual ICollection<QuotationDetail> QuotationDetails { get; set; } = new List<QuotationDetail>();
    
    }
}
