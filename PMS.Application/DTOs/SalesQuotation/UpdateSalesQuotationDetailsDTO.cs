using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class UpdateSalesQuotationDetailsDTO
    {
        public int? SqdId {  get; set; }
        public int? LotId { get; set; }
        public int? TaxId { get; set; }
        public int ProductId { get; set; }
        public string? Note { get; set; }
    }
}
