using PMS.Application.DTOs.SalesQuotation;
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
        public int Id { get; set; }
        public string QuotationCode { get; set; } = null!;
        public DateTime? QuotationDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public SalesQuotationStatus Status { get; set; }
        public decimal DepositPercent { get; set; }
        public int DepositDueDays { get; set; }
        public int RsqId { get; set; }
        public int SsId { get; set; }
        public int SqnId { get; set; }
        public string? Notes { get; set; }

        public List<SalesQuotationDetailsResponseDTO> Details { get; set; } = [];
    }
}
