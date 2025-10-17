using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Application.DTOs.Product
{
    public class ProductDTO
    {
        public int ProductID { get; set; }
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
        [StringLength(300, ErrorMessage = "Mô tả sản phẩm không được vượt quá 300 ký tự")]
        public string? Image { get; set; }

        [Required(ErrorMessage = "Số lượng tối thiểu là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối thiểu phải không âm")]
        public required int MinQuantity { get; set; }

        [Required(ErrorMessage = "Số lượng tối đa là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối đa phải không âm")]
        public required int MaxQuantity { get; set; }

        [Required(ErrorMessage = "Tổng số lượng hiện tại là bắt buộc, trong trường hợp tạo thủ công vui lòng nhập giá trị bằng 0")]
        [Range(0, int.MaxValue, ErrorMessage = "Tổng số lượng hiện tại phải không âm")]
        public required int TotalCurrentQuantity { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public bool Status { get; set; }
    }
}
