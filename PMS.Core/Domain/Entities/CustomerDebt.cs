using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class CustomerDebt
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int SalesOrderId { get; set; }
        public decimal DebtAmount { get; set; }

        public virtual SalesOrder SalesOrder { get; set; }
    }
}
