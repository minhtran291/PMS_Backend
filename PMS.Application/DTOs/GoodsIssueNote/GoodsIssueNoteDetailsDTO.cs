using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GoodsIssueNote
{
    public class GoodsIssueNoteDetailsDTO
    {
        public string WarehouseLocationName { get; set; } = string.Empty;
        public int LotId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime ExpiredDate { get; set; }
        public int Quantity { get; set; }
    }
}
