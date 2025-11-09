using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class InventoryCompareDTO
    {
        public int LotID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SystemQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public int Diff { get; set; }
        public string? Note { get; set; }
    }
}
