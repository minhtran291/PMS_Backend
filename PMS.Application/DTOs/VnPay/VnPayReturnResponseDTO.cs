using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VnPay
{
    public class VnPayReturnResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int SalesOrderId { get; set; }
        public string TxnRef { get; set; } = "";
    }
}
