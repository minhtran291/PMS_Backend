using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesOrderDepositCheck
    {
        public int Id { get; set; }

        public int SalesOrderId { get; set; }

        public decimal? RequestedAmount { get; set; }   // số tiền khách báo đã chuyển
        public PaymentMethod PaymentMethod { get; set; } // VnPay, Cash, BankTransfer

        public string? CustomerNote {  get; set; } // Lời nhắn của khách hàng

        public DepositCheckStatus Status { get; set; } // Pending, Approved, Rejected

        public string RequestedBy { get; set; }       // userId khách 
        public DateTime RequestedAt { get; set; }

        public string? CheckedBy { get; set; }        // userId kế toán
        public DateTime? CheckedAt { get; set; }
        public string? RejectReason { get; set; }

        public virtual SalesOrder SalesOrder { get; set; }
    }
}
