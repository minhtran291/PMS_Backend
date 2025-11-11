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
        public SalesOrderStatus Status { get; set; }
        public string StatusName { get; set; } = null!;
        public bool IsDeposited { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime SalesOrderExpiredDate { get; set; }
        public decimal PaidAmount { get; set; }
    }
}
