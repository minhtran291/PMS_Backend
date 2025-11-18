using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class InvoiceDetailDTO
    {
        public int GoodsIssueNoteId { get; set; }
        public DateTime GoodsIssueDate { get; set; }
        public decimal GoodsIssueAmount { get; set; }
        public decimal AllocatedDeposit { get; set; }
        public decimal PaidRemain { get; set; }
        public decimal TotalPaidForNote { get; set; }
        public decimal NoteBalance { get; set; }
        public int ExportIndex { get; set; }
    }
}
