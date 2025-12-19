using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum DepositCheckStatus : byte
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
