using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class StockExportOrderDetails
    {
        public int StockExportOrderId { get; set; }
        public int LotId { get; set; }
        public int Quantity { get; set; }

        public virtual StockExportOrder StockExportOrder { get; set; } = null!;
        public virtual LotProduct LotProduct { get; set; } = null!;
    }
}
