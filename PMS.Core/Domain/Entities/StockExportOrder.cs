using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class StockExportOrder
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public required string CreateBy { get; set; }
        public DateTime DueDate { get; set; } // han yeu cau tao phieu xuat
        public DateTime? RequestDate {  get; set; } // ngay gui yeu cau
        public StockExportOrderStatus Status { get; set; }

        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual User SalesStaff { get; set; } = null!;
        public virtual ICollection<StockExportOrderDetails> StockExportOrderDetails { get; set; } = [];
    }
}
