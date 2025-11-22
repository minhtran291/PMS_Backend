using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class DetailsStockExportOrderDTO
    {
        public int LotId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit {  get; set; } = string.Empty;
        public DateTime ExpiredDate { get; set; } 
        public int Quantity { get; set; }
        public int Available {  get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string WarehouseName {  get; set; } = string.Empty;

    }
}
