using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class CreateSalesQuotationDTO
    {
        public int RsqId { get; set; }
        public int NoteId { get; set; }
        public DateTime ExpiredDate { get; set; }
        [Range(0, 70, ErrorMessage = "Cọc trong khoảng từ 0% đến 70%")]
        public decimal DepositPercent { get; set; }
        [Range(1, 7, ErrorMessage = "Thời hạn thanh toán cọc trong khoảng từ 1 đến 7 ngày")]
        public int DepositDueDays { get; set; }
        public int Status { get; set; }
        public List<SalesQuotationDetailsDTO> Details { get; set; } = [];
    }
}
