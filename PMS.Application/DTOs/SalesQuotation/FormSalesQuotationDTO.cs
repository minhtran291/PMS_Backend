using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class FormSalesQuotationDTO
    {
        public int RsqId { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public List<SalesQuotationValidityDTO> Validities { get; set; } = [];
        public List<TaxPolicyDTO> Taxes { get; set; } = [];
        public List<LotDTO> LotProducts { get; set; } = [];
    }
}
