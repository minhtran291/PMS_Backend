using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;

namespace PMS.Core.Domain.Entities
{
    public class DebtReport
    {
        public int ReportID { get; set; }
        
        public decimal Payables { get; set; }// công nợ phải trả

        public int? EntityID { get;set; }// người nợ 

        public DebtEntityType EntityType { get; set; }// Phân loại đối tượng
        public DateTime? Payday { get; set; } // Ngày thanh toán thực tế

        public DebtStatus Status { get; set; } // trạng thái

        public decimal CurrentDebt { get; set; } // Dư nợ hiện tại

        public DateTime CreatedDate { get; set; } // ngày tạo


    }
}
