using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public sealed class FEFOLotDTO
    {
        public int LotId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public DateTime InputDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
    }
}
