using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesOrder
    {
        public string OrderId { get; set; }
        public int SalesQuotationId { get; set; }
        public int CustomerId { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public SalesOrderStatus Status { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal OrderTotalPrice { get; set; }

        public virtual ICollection<SalesOrderDetails> SalesOrderDetails { get; set; } = new List<SalesOrderDetails>();
        public virtual ICollection<CustomerDept> CustomerDepts { get; set; } = new List<CustomerDept>();
    }
}
