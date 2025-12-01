using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderDepositCheckItemDTO
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; } = null!;
        public decimal? RequestedAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DepositCheckStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
