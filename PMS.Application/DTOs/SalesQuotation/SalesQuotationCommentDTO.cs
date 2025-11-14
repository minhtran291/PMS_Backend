using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class SalesQuotationCommentDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string? Content { get; set; }
    }
}
