using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class CreateSalesOrderDepositCheckRequestDTO
    {
        public int SalesOrderId { get; set; }
        public decimal? RequestedAmount { get; set; } 
        public PaymentMethod PaymentMethod { get; set; }
        public string? CustomerNote { get; set; }
    }
}
