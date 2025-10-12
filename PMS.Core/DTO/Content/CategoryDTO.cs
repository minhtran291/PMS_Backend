using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Content
{
    public class CategoryDTO
    {
        public int CategoryID { get; set; }
        [Required(ErrorMessage = "Tên của loại sản phẩm là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Đảm bảo 6 ký tự")]
        public required string Name { get; set; }
        [StringLength(300, ErrorMessage = "Mô tả loại sản phẩm không được vượt quá 300 ký tự")]
        public string? Description { get; set; }
    }
}
