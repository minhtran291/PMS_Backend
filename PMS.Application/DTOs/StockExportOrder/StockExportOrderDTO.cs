using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class StockExportOrderDTO
    {
        public int SalesOrderId { get; set; }
        public DateTime DueDate { get; set; }
        public int Status {  get; set; }
        public List<StockExportOrderDetailsDTO> Details { get; set; } = [];
    }
}
