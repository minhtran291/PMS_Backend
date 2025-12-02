using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class ProductNearestLotDto
    {
        
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalCurrentQuantity { get; set; }

        
        public int LotID { get; set; }
        public DateTime InputDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public int LotQuantity { get; set; }
        public decimal InputPrice { get; set; }
        public decimal SalePrice { get; set; }

       
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        
        public int WarehouseLocationID { get; set; }
        public string WarehouseLocationName { get; set; } = string.Empty;
    }
}
