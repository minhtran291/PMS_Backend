using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class ListStockExportOrderDTO
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; } = string.Empty;
        public string CreateBy { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime? RequestDate { get; set; }
        public StockExportOrderStatus Status { get; set; }
    }
}
