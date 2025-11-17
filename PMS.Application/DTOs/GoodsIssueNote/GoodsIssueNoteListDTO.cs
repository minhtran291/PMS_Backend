using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GoodsIssueNote
{
    public class GoodsIssueNoteListDTO
    {
        public int Id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string GoodsIssueNoteCode { get; set; } = string.Empty;
        public string StockExportOrderCode { get; set; } = string.Empty;
        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
        public DateTime? ExportedAt { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string? Note { get; set; }
        public GoodsIssueNoteStatus Status { get; set; }
    }
}
