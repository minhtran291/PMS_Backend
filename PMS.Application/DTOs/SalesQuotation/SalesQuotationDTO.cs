using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class SalesQuotationDTO
    {
        public int Id { get; set; }
        public string RequestCode {  get; set; } = string.Empty;
        public string QuotationCode { get; set; } = string.Empty;
        public SalesQuotationStatus Status { get; set; }
    }
}
