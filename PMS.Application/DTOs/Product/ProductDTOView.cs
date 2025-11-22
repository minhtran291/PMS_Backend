using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PMS.Application.DTOs.Product
{
    public class ProductDTOView
    {
        [Required(ErrorMessage = "Tên của sản phẩm là bắt buộc")]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "Tên sản phẩm phải từ 10 đến 100 ký tự")]
        public required string ProductName { get; set; }

        [StringLength(300, ErrorMessage = "Mô tả sản phẩm không được vượt quá 300 ký tự")]
        public string? ProductDescription { get; set; }

        [Required(ErrorMessage = "Đơn vị tính là bắt buộc")]
        [StringLength(10, ErrorMessage = "Đơn vị tính không được vượt quá 10 ký tự")]
        public required string Unit { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        [ForeignKey("Category")]
        public required int CategoryID { get; set; }

        public IFormFile? Image { get; set; }

        [Required(ErrorMessage = "Số lượng tối thiểu là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối thiểu phải không âm")]
        public required int MinQuantity { get; set; }

        [Required(ErrorMessage = "Số lượng tối đa là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối đa phải không âm")]
        public required int MaxQuantity { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public bool Status { get; set; }
    }
}
