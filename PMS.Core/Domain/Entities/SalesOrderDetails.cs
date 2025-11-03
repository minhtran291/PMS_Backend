using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesOrderDetails
    {
        public int SalesOrderDetailsId { get; set; }
        public string SalesOrderId { get; set; }
        public int LotId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }


        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual LotProduct Lot { get; set; } = null!;
    }
}
