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
        public string RequestCode { get; set; } = null!;
        public DateTime? RequestDate { get; set; }
        public RequestSalesQuotationStatus Status { get; set; }
        public List<ViewRsqDetailsDTO> Details { get; set; } = [];
        public int? SalesQuotationId { get; set; }
        public ViewRsqSalesQuotationDTO? SalesQuotation { get; set; }
        public List<ViewRsqSalesQuotationDTO> SalesQuotations { get; set; } = [];
    }

    public class ViewRsqSalesQuotationDTO
    {
        public int Id { get; set; }
        public string QuotationCode { get; set; } = string.Empty;
        public DateTime? QuotationDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public SalesQuotationStatus Status { get; set; }
    }
}
