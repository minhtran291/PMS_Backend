using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class UpdateSalesQuotationDTO
    {
        public int SqId {  get; set; }
        public int SqvId { get; set; }
        public List<SalesQuotationDetailsDTO> Details { get; set; } = [];
    }
}
