using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class LotProductDTO
    {
        public int LotID { get; set; }
        public DateTime InputDate { get; set; }
        public decimal SalePrice { get; set; }
        public decimal InputPrice { get; set; }
        public DateTime ExpiredDate { get; set; }
        public int LotQuantity { get; set; }
        //public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        //public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime LastedUpdate { get; set; }
        public string? InventoryBy { get; set; }
        public string? note { get; set; }
        public int DiffQuantity {  get; set; }

    }
}
