using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VietQR
{
    public class VietQrConfirmRequest
    {
        public int SalesOrderId { get; set; }
        public string PaymentType { get; set; } = "deposit";
        public decimal? AmountReceived { get; set; }
    }
}
