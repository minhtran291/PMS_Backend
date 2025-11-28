using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PMS.Application.DTOs.Product
{
    public class ProductUpdateDTO
    {
        [StringLength(100, MinimumLength = 10, ErrorMessage = "Tên sản phẩm phải từ 10 đến 100 ký tự")]
        public string? ProductName { get; set; }

        [StringLength(300, ErrorMessage = "Mô tả sản phẩm không được vượt quá 300 ký tự")]
        public string? ProductDescription { get; set; }

        public IFormFile? Image { get; set; }
        public IFormFile? ImageA { get; set; }
        public IFormFile? ImageB { get; set; }
        public IFormFile? ImageC { get; set; }
        public IFormFile? ImageD { get; set; }
        public IFormFile? ImageE { get; set; }

        [StringLength(10, ErrorMessage = "Đơn vị tính không được vượt quá 10 ký tự")]
        public string? Unit { get; set; }

        public int CategoryID { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối thiểu phải không âm")]
        public int MinQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối đa phải không âm")]
        public int MaxQuantity { get; set; }

        public bool Status { get; set; }
    }
}
