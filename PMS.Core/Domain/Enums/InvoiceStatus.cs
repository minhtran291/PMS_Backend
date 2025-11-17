using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum InvoiceStatus : byte
    {
        Draft = 0,
        Send = 1,
        Cancelled = 2
    }
}
