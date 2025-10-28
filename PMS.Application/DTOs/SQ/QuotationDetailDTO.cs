using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SQ
{
    public class QuotationDetailDTO
    {
        public required int ProductID { get; set; }

        public required string ProductName { get; set; }
        public required string ProductDescription { get; set; }
        public required string ProductUnit { get; set; }
        public required decimal UnitPrice { get; set; }
        public required DateTime ProductDate { get; set; }
    }
}
