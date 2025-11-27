using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class RejectSalesOrderRequestDTO
    {
        public int SalesOrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
