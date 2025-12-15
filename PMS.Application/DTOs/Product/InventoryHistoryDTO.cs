using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class InventoryHistoryDTO
    {
        public int InventoryHistoryID { get; set; }
        public int LotID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SystemQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public int Diff => ActualQuantity - SystemQuantity;
        public string? Note { get; set; }
        public string? InventoryBy { get; set; }
        public string? InventoryById { get; set; }   // giữ userId gốc từ DB
        public string? InventoryByName { get; set; } // tên người kiểm kê (FullName)
        public DateTime LastUpdated { get; set; }
    }
}
