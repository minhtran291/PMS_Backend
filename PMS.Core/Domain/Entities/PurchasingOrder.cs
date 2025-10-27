using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;

namespace PMS.Core.Domain.Entities
{
    public class PurchasingOrder
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int POID { get; set; }
        public decimal Total { get; set; }
        [Required(ErrorMessage = "Ngày Order không được phép bỏ trống")]
        public required DateTime OrderDate { get; set; }
        public string? PaymentBy { get; set; }
        public PurchasingOrderStatus Status { get; set; }
        public decimal Deposit { get; set; }
        public decimal Debt { get; set; }
        public DateTime PaymentDate { get; set; }
        public string UserId { get; set; }// người tạo
        [Required(ErrorMessage = "QID không được phép bỏ trống")]
        [ForeignKey("Quotation")]
        public required int QID { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<PurchasingOrderDetail> PurchasingOrderDetails { get; set; } = new List<PurchasingOrderDetail>();
        public virtual ICollection<GoodReceiptNote> GoodReceiptNotes { get; set; } = new List<GoodReceiptNote>();

        public virtual Quotation Quotations { get; set; } = null;

        //public virtual ICollection<SupplierInvoice> SupplierInvoices { get; set; } = new List<SupplierInvoice>();
    }
}
