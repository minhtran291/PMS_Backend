using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PRFQ
{
    public class ContinuePRFQDTO
    {
        [Required(ErrorMessage = "Danh sách sản phẩm không được để trống.")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm.")]
        public List<int> ProductIds { get; set; } = new();

        public required PRFQStatus PRFQStatus { get; set; }
    }
}
