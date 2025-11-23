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
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; }
        public int SalesOrderId { get; set; }

        public PaymentType PaymentType { get; set; }   // Deposit, Remain, Full
        public PaymentMethod PaymentMethod { get; set; } // VnPay, VietQR, Cash...

        public decimal Amount { get; set; } // số tiền thanh toán lần này
        public DateTime CreateRequestAt { get; set; } //  Thời điểm tạo yêu cầu cho khách thanh toán
        public DateTime? PaidAt { get; set; } // Thời điểm thanh toán

        public VNPayStatus VNPayStatus { get; set; }

        // Thông tin từ cổng thanh toán (nếu có)
        public string? GatewayTransactionRef { get; set; }
        public string? Gateway { get; set; }  

        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual Invoice Invoice { get; set; } = null!;
    }
}
