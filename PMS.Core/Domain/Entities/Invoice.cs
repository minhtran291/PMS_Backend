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

        public InvoiceStatus Status { get; set; }      // Issued, Cancelled...

        public decimal TotalAmount { get; set; }       // = SalesOrder.TotalPrice
        public decimal TotalPaid { get; set; }         // = tổng payments (Success)
        public decimal TotalDeposit { get; set; }      // tổng PaymentType = Deposit
        public decimal TotalRemain { get; set; }       // tổng PaymentType = Remain / Full

        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
    }
}
