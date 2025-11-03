using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class CreateSalesQuotationDTO
    {
        public int RsqId { get; set; }
        public int NoteId { get; set; }
        public DateTime ExpiredDate { get; set; }
        public List<SalesQuotationDetailsDTO> Details { get; set; } = [];
    }
}
