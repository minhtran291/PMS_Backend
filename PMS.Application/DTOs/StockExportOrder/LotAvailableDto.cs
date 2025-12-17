using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class LotAvailableDto
    {
        public decimal SalePrice { get; set; }
        public decimal InputPrice { get; set; }
        public DateTime ExpiredDate { get; set; }
        public int SupplierID { get; set; }
        public int ProductID { get; set; }
        public int TotalQuantity { get; set; }
    }
}
