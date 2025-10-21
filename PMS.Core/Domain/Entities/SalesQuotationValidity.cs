using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class SalesQuotationValidity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // ví dụ: "Hạn 15 ngày", "Hạn 30 ngày"
        public string Content { get; set; } = string.Empty; // ví dụ: "Báo giá có hiệu lực trong 15 ngày kể từ ngày phát hành"
        public int Days { get; set; } // 15, 30, 60
        public bool Status { get; set; }

        public virtual SalesQuotation SalesQuotation { get; set; } = null!;
    }
}
