using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class CreateOrderFromQuotationItemDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }  
        public decimal? UnitPrice { get; set; }
    }
}
