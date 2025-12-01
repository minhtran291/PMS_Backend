using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderDepositCheckDetailDTO
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; } = null!;
        public decimal TotalOrderAmount { get; set; }

        public decimal? RequestedAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? CustomerNote { get; set; }

        public DepositCheckStatus Status { get; set; }

        public string RequestedBy { get; set; } = null!;
        public DateTime RequestedAt { get; set; }

        public string? CheckedBy { get; set; }
        public DateTime? CheckedAt { get; set; }
        public string? RejectReason { get; set; }
    }
}
