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
        public int SqnId { get; set; }
        public DateTime ExpiredDate { get; set; }
        public List<UpdateSalesQuotationDetailsDTO> Details { get; set; } = [];
    }
}
