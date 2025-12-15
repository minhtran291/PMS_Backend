using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class InvoiceDTO
    {
        public int Id { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public int SalesOrderId { get; set; }
        public string  SalesOrderCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime IssuedAt { get; set; }
        public InvoiceStatus Status { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalRemain { get; set; }

        public List<InvoiceDetailDTO> Details { get; set; } = new();
    }
}
