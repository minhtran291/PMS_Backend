using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class Quotation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required int QID { get; set; }
        public required DateTime SendDate { get; set; }
        public required int SupplierID { get; set; }
        public bool Status { get; set; }
        public required DateTime QuotationExpiredDate { get; set; }
        public virtual ICollection<PurchasingOrder> PurchasingOrders { get; set; } = new List<PurchasingOrder>();
        public virtual ICollection<QuotationDetail> QuotationDetails { get; set; } = new List<QuotationDetail>();
    }
}
