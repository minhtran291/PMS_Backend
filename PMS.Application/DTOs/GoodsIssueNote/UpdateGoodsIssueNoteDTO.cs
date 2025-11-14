using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GoodsIssueNote
{
    public class UpdateGoodsIssueNoteDTO
    {
        public int GoodsIssueNoteId {  get; set; }
        public string? Note { get; set; }
    }
}
