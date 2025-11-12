using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum CustomerDebtStatus : byte
    {
        UnPaid = 0,
        OnTime = 1,
        OverTime = 2
    }
}
