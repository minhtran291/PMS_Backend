using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesOrder
    {
        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; }
        public int SalesQuotationId { get; set; }
        public required string CreateBy { get; set; }
        public DateTime CreateAt { get; set; }
        public SalesOrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsDeposited { get; set; }

        public virtual ICollection<SalesOrderDetails> SalesOrderDetails { get; set; } = [];
        //public virtual ICollection<CustomerDept> CustomerDepts { get; set; } = new List<CustomerDept>();
        public virtual SalesQuotation SalesQuotation { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
    }
}
