using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class RejectSalesOrderDepositCheckDTO
    {
        public int RequestId { get; set; }
        public string Reason { get; set; } = null!;
    }
}
