using PMS.Application.DTOs.RequestSalesQuotationDetails;
using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.RequestSalesQuotation
{
    public class ViewRsqDTO
    {
        public int Id {  get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RequestCode { get; set; } = null!;
        public DateTime? RequestDate { get; set; }
        public RequestSalesQuotationStatus Status { get; set; }
        public List<ViewRsqDetailsDTO> Details { get; set; } = [];
    }
}
