using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderItemDTO
    {
        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; } = null!;
        public string? CustomerName { get; set; }
        public SalesOrderStatus SalesOrderStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string SalesOrderStatusName { get; set; } = null!;
        public string PaymentStatusName { get; set; } = null!;
        public bool IsDeposited { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime SalesOrderExpiredDate { get; set; }
        public DateTime? PaidFullAt { get; set; }
        public decimal PaidAmount { get; set; }
        public string RejectReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectBy { get; set; }
    }
}
