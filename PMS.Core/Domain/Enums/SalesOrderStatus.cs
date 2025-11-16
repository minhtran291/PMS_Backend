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
        Approved =2,
        Rejected = 3,
        Delivered = 4,
        Complete = 5,
        NotComplete = 6,

    }
}
