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

        [Required(ErrorMessage = "Deposit không được bỏ trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Số nhập phải lớn hơn 0")]
        [Column(TypeName = "decimal(18, 2)")]
        public required decimal paid { get; set; }

    }
}
