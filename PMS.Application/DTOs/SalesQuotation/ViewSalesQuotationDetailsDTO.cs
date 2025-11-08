using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class ViewSalesQuotationDetailsDTO
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string? TaxText {  get; set; }
        public string ExpiredDate { get; set; } = string.Empty;
        public int minQuantity { get; set; } = 1;
        public decimal? SalesPrice { get; set; }
        public decimal? ItemTotal { get; set; }  // Tổng cộng (bao gồm thuế)
        public string? Note { get; set; }
    }
}
