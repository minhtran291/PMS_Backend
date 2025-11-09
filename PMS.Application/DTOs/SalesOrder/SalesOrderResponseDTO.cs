using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderResponseDTO
    {
        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; }
        public int SalesQuotationId { get; set; }
        public required string CreateBy { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public SalesOrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsDeposited { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotalPrice { get; set; }
    }
}
