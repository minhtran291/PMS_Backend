using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class GoodsIssueNoteDetails
    {
        public int GoodsIssueNoteId { get; set; }
        public int LotId { get; set; }
        public int Quantity {  get; set; }

        public virtual GoodsIssueNote GoodsIssueNote { get; set; } = null!;
        public virtual LotProduct LotProduct { get; set; } = null!;
    }
}
