using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class PaymentRemain
    {
        public int Id { get; set; }

        public int SalesOrderId { get; set; }

        // null = thanh toán cho toàn order (đặt cọc),
        // có giá trị = thanh toán phần còn lại cho 1 phiếu xuất cụ thể
        public int? GoodsIssueNoteId { get; set; }

        public PaymentType PaymentType { get; set; }   // Deposit, Remain, Full
        public PaymentMethod PaymentMethod { get; set; } // VnPay, VietQR, Cash...

        public decimal Amount { get; set; }            // số tiền thanh toán lần này
        public DateTime CreateRequestAt { get; set; } 

        public PaymentStatus Status { get; set; }      // Pending, Success, Failed...

        // Thông tin từ cổng thanh toán (nếu có)
        public string? GatewayTransactionRef { get; set; }
        public string? Gateway { get; set; }  

        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual GoodsIssueNote? GoodsIssueNote { get; set; }
    }
}
