using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class UpdateSalesQuotationDetailsDTO
    {
        public int sqdId {  get; set; }
        public int? TaxId { get; set; }
        public string? Note { get; set; }
    }
}
