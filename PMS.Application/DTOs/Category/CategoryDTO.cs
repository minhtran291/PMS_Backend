using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Category
{
    public class CategoryDTO
    {
        public int CategoryID { get; set; }
        [Required(ErrorMessage = "Tên của loại sản phẩm là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Đảm bảo 6 ký tự")]
        public required string Name { get; set; }
        [StringLength(300, ErrorMessage = "Mô tả loại sản phẩm không được vượt quá 300 ký tự")]
        public string? Description { get; set; }
        public bool Status {  get; set; }
        public virtual ICollection<Product.ProductDTO> Products { get; set; }=new List<Product.ProductDTO>();
    }
}
