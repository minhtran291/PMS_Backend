using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Content
{
    public class ProductUpdateDTO
    {
        [StringLength(100, MinimumLength = 10, ErrorMessage = "Tên sản phẩm phải từ 10 đến 100 ký tự")]
        public string? ProductName { get; set; }

        [StringLength(300, ErrorMessage = "Mô tả sản phẩm không được vượt quá 300 ký tự")]
        public string? ProductDescription { get; set; }

        [StringLength(10, ErrorMessage = "Đơn vị tính không được vượt quá 10 ký tự")]
        public string? Unit { get; set; }

        public int CategoryID { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal InputPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối thiểu phải không âm")]
        public int MinQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối đa phải không âm")]
        public int MaxQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tổng số lượng hiện tại phải không âm")]
        public int TotalCurrentQuantity { get; set; }

        public bool Status { get; set; }
    }
}
