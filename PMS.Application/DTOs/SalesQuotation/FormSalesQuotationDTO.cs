using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class FormSalesQuotationDTO
    {
        public List<SalesQuotationValidityDTO> Validities { get; set; } = [];
        public List<TaxPolicyDTO> Taxes { get; set; } = [];
        public List<LotDTO> LotProducts { get; set; } = [];
    }
}
