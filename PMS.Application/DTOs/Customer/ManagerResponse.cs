using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Customer
{
    public class ManagerResponse
    {
        public required UserStatus Status { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập lý do")]
        [StringLength(256, ErrorMessage = "Lý do không vượt quá 256 ký tự")]
        public required string note { get; set; }
    }
}
