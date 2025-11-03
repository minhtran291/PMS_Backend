using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class AddSalesQuotationCommentDTO
    {
        public int SqId { get; set; }
        public string? Content { get; set; }
    }
}
