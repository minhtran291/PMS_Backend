using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public required string InvoiceCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime IssuedAt { get; set; }

        public InvoiceStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }  

        public decimal TotalAmount { get; set; }       // = SalesOrder.TotalPrice
        public decimal TotalPaid { get; set; }         // Số tiền khách đã cọc + thanh toán các lần khác
        public decimal TotalDeposit { get; set; }      // Số tiền cọc đã phân bổ vào Invoice này
        public decimal TotalRemain { get; set; } //Số tiền còn lại phải thu = TotalAmount - TotalPaid

        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
        public virtual ICollection<PaymentRemain> PaymentRemains { get; set; } = [];
    }
}
