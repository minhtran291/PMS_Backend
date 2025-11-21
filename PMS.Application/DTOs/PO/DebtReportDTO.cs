using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PO
{
    public class DebtReportDTO
    {
        public int ReportID { get; set; }

        public decimal Payables { get; set; }// công nợ phải trả

        public int? EntityID { get; set; }// người nợ 

        public string DebtName {  get; set; }

        public DebtEntityType EntityType { get; set; }// Phân loại đối tượng
        public DateTime? Payday { get; set; } // Ngày thanh toán thực tế

        public DebtStatus Status { get; set; } // trạng thái

        public decimal CurrentDebt { get; set; } // Dư nợ hiện tại

        public DateTime CreatedDate { get; set; } // ngày tạo

        public ICollection<ViewDebtPODTO> viewDebtPODTOs { get; set; } = new List<ViewDebtPODTO>();
    }
}
