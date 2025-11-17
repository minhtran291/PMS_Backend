using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class GoodsIssueNote
    {
        public int Id { get; set; }
        public int StockExportOrderId { get; set; }
        public required string GoodsIssueNoteCode { get; set; }
        public required string CreateBy { get; set; }
        public int WarehouseId {  get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? ExportedAt { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string? Note { get; set; }
        public GoodsIssueNoteStatus Status { get; set; }

        public virtual StockExportOrder StockExportOrder { get; set; } = null!;
        public virtual User WarehouseStaff { get; set; } = null!;
        public virtual ICollection<GoodsIssueNoteDetails> GoodsIssueNoteDetails { get; set; } = [];
        public virtual Warehouse Warehouse {  get; set; } = null!;
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; }
        = new List<InvoiceDetail>();
        public virtual PaymentRemain? PaymentRemain { get; set; }
    }
}
