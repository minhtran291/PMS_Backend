using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum SalesOrderStatus : byte
    {
        Draft = 0,
        Send = 1,
        Approve =2,
        Reject = 3,
        Deposited = 4,
        Paid = 5,
        Complete = 6
    }
}
