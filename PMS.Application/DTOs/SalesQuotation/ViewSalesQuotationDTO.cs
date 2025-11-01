using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class ViewSalesQuotationDTO : SalesQuotationDTO
    {
        public List<ViewSalesQuotationDetailsDTO> Details { get; set; } = [];
        public List<SalesQuotationCommentDTO> Comments { get; set; } = [];
        public decimal? subTotal { get; set; }
        public decimal? taxTotal { get; set; }
        public decimal? grandTotal { get; set; }
        public string? note {  get; set; }
    }
}
