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

        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;

        public int SalesOrderId { get; set; }
        public string? SalesOrderCode { get; set; }

        public PaymentType PaymentType { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public VNPayStatus VNPayStatus { get; set; }

        public decimal Amount { get; set; }

        public DateTime? RequestCreatedAt { get; set; }  // Ngày tạo yêu cầu thanh toán
        public DateTime? PaidAt { get; set; }

        public string? GatewayTransactionRef { get; set; }
        public string? Gateway { get; set; }
        public decimal SalesOrderTotalPrice { get; set; }
        public decimal SalesOrderPaidAmount { get; set; }

        public string? CustomerId { get; set; }          // ID Khách hàng
        public string? CustomerName { get; set; }        // Tên khách hàng
      
        public string? PaymentStatusText { get; set; } 
    }
}