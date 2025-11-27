using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesOrder
    {
        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; }
        public int SalesQuotationId { get; set; }
        public required string CreateBy { get; set; }
        public DateTime CreateAt { get; set; }
        public SalesOrderStatus SalesOrderStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsDeposited { get; set; }
        public required DateTime SalesOrderExpiredDate { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? PaidFullAt { get; set; }
        public string? RejectReason { get; set; } 
        public DateTime? RejectedAt { get; set; }  
        public string? RejectedBy { get; set; } 


        public virtual ICollection<SalesOrderDetails> SalesOrderDetails { get; set; } = [];
        public virtual CustomerDebt CustomerDebts { get; set; }
        public virtual SalesQuotation SalesQuotation { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
        public virtual ICollection<StockExportOrder> StockExportOrders { get; set; } = [];
        public virtual ICollection<PaymentRemain> PaymentRemains { get; set; } = [];
        public virtual ICollection<Invoice> Invoice { get; set; } = new List<Invoice>();

    }
}
