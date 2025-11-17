using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PaymentRemain
{
    public class PaymentRemainItemDTO
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public int? GoodsIssueNoteId { get; set; }
        public PaymentType PaymentType { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? GatewayTransactionRef { get; set; }
        public string? Gateway { get; set; }
        public string? SalesOrderCode { get; set; }
        public decimal SalesOrderTotalPrice { get; set; }
        public decimal SalesOrderPaidAmount { get; set; }
    }
}