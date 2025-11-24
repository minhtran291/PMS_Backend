using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PaymentRemain
{
    public class CreatePaymentRemainRequestDTO
    {
        public int InvoiceId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } // VnPay, Cash, BankTransfer...

        public PaymentType PaymentType { get; set; } = PaymentType.Remain;

        public decimal? Amount { get; set; } //Số tiền khách sẽ thanh toán. Nếu null thì mặc định = phần còn lại của Invoice (TotalRemain).
    }
}
