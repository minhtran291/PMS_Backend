using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum PaymentStatus : byte
    {
        Pending = 0,
        Deposited = 1,
        Paid = 2,
        Success = 3,
        Failed = 4,
        Refunded = 5
    }
}
