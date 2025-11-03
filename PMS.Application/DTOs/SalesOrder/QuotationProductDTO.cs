using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class QuotationProductDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public decimal ProductQuantity { get; set; }
        public string ProductDescription { get; set; }
        public string ProductUnit { get; set; }
        public DateTime ProductDate { get; set; }
    }
}
