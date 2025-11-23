using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class GenerateInvoiceFromGINRequestDTO
    {
        public string SalesOrderCode { get; set; }
        public List<string> GoodsIssueNoteCodes { get; set; } = new();
    }
}
