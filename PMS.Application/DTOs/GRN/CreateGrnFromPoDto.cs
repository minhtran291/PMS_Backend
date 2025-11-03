using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GRN
{
    public class CreateGrnFromPoDto
    {
        [Required(ErrorMessage ="Mã kho chứa là bắt buộc")]
        public int WarehouseLocationID { get; set; }
    }
}
