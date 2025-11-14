using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class InvoiceDetail
    {
        public int InvoiceId { get; set; }
        public int GoodsIssueNoteId { get; set; }

        public decimal GoodsIssueAmount { get; set; }      // giá trị hàng của phiếu xuất
        public decimal AllocatedDeposit { get; set; }      // phần cọc chia cho phiếu này
        public decimal PaidRemain { get; set; }            // tiền remain đã trả cho phiếu này
        public decimal TotalPaidForNote { get; set; }      // = AllocatedDeposit + PaidRemain
        public decimal NoteBalance { get; set; }           // còn thiếu (thường = 0 nếu ra Invoice lúc đã full paid)

        public virtual Invoice Invoice { get; set; } = null!;
        public virtual GoodsIssueNote GoodsIssueNote { get; set; } = null!;
    }
}
