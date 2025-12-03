using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum DepositCheckStatus : byte
    {
        Draft = 0,
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}
