using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum PaymentType : byte
    {
        Deposit = 0,
        Remain = 1,
        Full = 2
    }
}
