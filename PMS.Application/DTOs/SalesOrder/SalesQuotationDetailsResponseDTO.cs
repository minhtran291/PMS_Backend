using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesQuotationDetailsResponseDTO
    {
        public int SalesQuotationDetailsId { get; set; }      
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductUnit { get; set; }
        public string? ProductDescription { get; set; }

        public int? LotId { get; set; }
        public decimal? UnitPrice { get; set; }
        public DateTime? LotInputDate { get; set; }
        public DateTime? LotExpiredDate { get; set; }

        public int? TaxId { get; set; }
        public string? Note { get; set; }
    }
}
