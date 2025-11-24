using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PaymentRemain
{
    public class MarkPaymentSuccessRequestDTO
    {
        public string? GatewayTransactionRef { get; set; }
    }
}
