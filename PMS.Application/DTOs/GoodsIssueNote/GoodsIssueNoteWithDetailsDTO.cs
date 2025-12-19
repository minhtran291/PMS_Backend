using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GoodsIssueNote
{
    public class GoodsIssueNoteWithDetailsDTO : GoodsIssueNoteListDTO
    {
        public List<GoodsIssueNoteDetailsDTO> Details { get; set; } = [];
        public string SalesOrderCode { get; set; } = string.Empty;
    }
}
