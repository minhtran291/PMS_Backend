using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VietQR
{
    public class VietQrInitRequest
    {
        public int SalesOrderId { get; set; }
        public string PaymentType { get; set; } = "deposit";
    }
}
