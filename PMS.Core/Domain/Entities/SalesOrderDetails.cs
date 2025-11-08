using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesOrderDetails
    {
        public int SalesOrderId { get; set; }
        public int ProductId { get; set; } 
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotalPrice { get; set; }

        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
