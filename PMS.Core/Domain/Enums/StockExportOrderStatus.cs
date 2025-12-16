using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum StockExportOrderStatus : byte
    {
        Draft = 0,
        Sent = 1,
        Exported = 2,
        NotEnough = 3,
        Await = 4,
        Cancel = 5,
        ReadyToExport = 6,
    }
}
