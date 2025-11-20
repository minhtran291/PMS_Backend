using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class UpdateStockExportOrderDTO
    {
        public int StockExportOrderId { get; set; }
        public DateTime DueDate { get; set; }
        public int Status { get; set; }
        public List<StockExportOrderDetailsDTO> Details { get; set; } = [];
    }
}
