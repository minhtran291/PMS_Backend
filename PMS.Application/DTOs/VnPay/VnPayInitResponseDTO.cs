using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VnPay
{
    public class VnPayInitResponseDTO
    {
        public string PaymentUrl { get; set; } = string.Empty;
        public string QrBase64 { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string TxnRef { get; set; } = "";
    }
}
