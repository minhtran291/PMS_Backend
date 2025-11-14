using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum PaymentMethod : byte
    {
        VnPay = 1,
        Cash= 2,
        BankTransfer = 3 // có thể trong trường hợp VietQR
    }
}
