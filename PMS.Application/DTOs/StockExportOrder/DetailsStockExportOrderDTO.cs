using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class DetailsStockExportOrderDTO
    {
        public string ProductName { get; set; } = string.Empty;
        public DateTime ExpiredDate { get; set; } 
        public int Quantity { get; set; }
    }
}
