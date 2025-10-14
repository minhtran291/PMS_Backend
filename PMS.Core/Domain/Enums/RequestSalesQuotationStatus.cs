using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum RequestSalesQuotationStatus : byte
    {
        Draft = 0,
        Sent = 1,
        Quoted = 2
    }
}
