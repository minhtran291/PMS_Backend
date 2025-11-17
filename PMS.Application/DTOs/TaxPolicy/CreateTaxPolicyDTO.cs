using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.TaxPolicy
{
    public class CreateTaxPolicyDTO
    {
        [Required (ErrorMessage = "Tên thuế xuất không được để chống")]
        public string Name { get; set; } = string.Empty;
        [Range(0, 1, ErrorMessage = "Thuế chỉ được phép trong khoảng từ 0 đến 1")]
        public decimal Rate { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; }
    }
}
