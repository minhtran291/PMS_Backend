using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class CustomerDept
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string SalesOrderId { get; set;}
        public decimal DeptAmount { get; set; }

        public virtual SalesOrder SalesOrder { get; set; } = null!;
    }
}
