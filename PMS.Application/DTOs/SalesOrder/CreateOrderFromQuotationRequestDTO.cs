using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class CreateOrderFromQuotationRequestDTO
    {
        public int SalesQuotationId { get; set; }
        public string CreatedBy { get; set; } = null!;
        public List<CreateOrderFromQuotationItemDTO> Items { get; set; } = [];
    }
}
