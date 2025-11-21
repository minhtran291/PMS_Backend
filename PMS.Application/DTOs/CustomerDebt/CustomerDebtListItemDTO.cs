using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.CustomerDebt
{
    public class CustomerDebtListItemDTO
    {
        public int Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;

        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; } = string.Empty;

        public CustomerDebtStatus Status { get; set; }
        public DateTime DueDate { get; set; }              // Hạn trả nợ (SalesOrder.SalesOrderExpiredDate)
        public decimal TotalAmount { get; set; }           // Tổng tiền phải trả (SalesOrder.TotalPrice)
        public decimal DebtAmount { get; set; }            // Số tiền còn nợ (CustomerDebt.DebtAmount)

        public string? CustomerName { get; set; }
    }
}
