using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class POUpdateDTO
    {

        public bool Status { get; set; }
        [Required(ErrorMessage = "Deposit không được bỏ trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Số nhập phải lớn hơn 0")]
        [Column(TypeName = "decimal(18, 2)")]
        public required decimal Deposit { get; set; }
        public decimal Debt { get; set; }
        [Required(ErrorMessage = "Ngày trả không được bỏ trống")]
        public required DateTime PaymentDate { get; set; }

    }
}
