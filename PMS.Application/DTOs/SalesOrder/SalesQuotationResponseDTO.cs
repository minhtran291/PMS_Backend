using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesQuotationResponseDTO
    {
        public int SalesQuotationId { get; set; }
        public int SalesQuotationCode { get; set; }
        public DateTime? QuotationDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public SalesQuotationStatus Status { get; set; }
        public decimal DepositPercent { get; set; }
        public int DepositDueDays { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ProductUnit { get; set; }
        public decimal UnitPrice { get; set; }
        public string ProductDescription { get; set; }
        public DateTime ProductDate { get; set; }
    }
}
