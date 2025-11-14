using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        public required string ProductName { get; set; }

        public string? ProductDescription { get; set; }

        public string? Image { get; set; }

        public required string Unit { get; set; }

        [ForeignKey("CategoryID")]
        public int CategoryID { get; set; }
        public required int MinQuantity { get; set; }

        public required int MaxQuantity { get; set; }

        public required int TotalCurrentQuantity { get; set; }

        public required bool Status { get; set; } = false;

        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<RequestSalesQuotationDetails> RequestSalesQuotationDetails { get; set; } = [];

        public virtual ICollection<PurchasingRequestProduct> PRPS { get; set; } = new List<PurchasingRequestProduct>();
        public virtual ICollection<LotProduct> LotProducts { get; set; } = new List<LotProduct>();
        public virtual ICollection<GoodReceiptNoteDetail> GoodReceiptNoteDetails { get; set; } = new List<GoodReceiptNoteDetail>();
        public virtual ICollection<SalesQuotaionDetails> SalesQuotaionDetails { get; set; } = [];
        //public virtual ICollection<SalesOrderDetails> SalesOrderDetails { get; set; } = [];
    }
}
