using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Customer
{
    public class CustomerProfileDTO
    {
        [Required(ErrorMessage = "Mã số thuế (Mst) là bắt buộc.")]
        [Range(1, long.MaxValue, ErrorMessage = "Mst phải lớn hơn 0.")]
        [Display(Name = "Mã số thuế")]
        public required long? Mst { get; set; }

        [Required(ErrorMessage = "Ảnh Chứng nhận Kinh doanh là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Đường dẫn ảnh CNKD không được vượt quá 255 ký tự.")]
        [Display(Name = "Ảnh CNKD")]
        public required string ImageCnkd { get; set; }

        [Required(ErrorMessage = "Ảnh chứng nhận của bộ y tế (Byt) là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Đường dẫn ảnh BYT không được vượt quá 255 ký tự.")]
        [Display(Name = "Ảnh BYT")]
        public required string ImageByt { get; set; }

        [Required(ErrorMessage = "Mã số hộ kinh doanh (Mshkd) là bắt buộc.")]
        [Range(1, long.MaxValue, ErrorMessage = "Mshkd phải lớn hơn 0.")]
        [Display(Name = "Mã số hợp đồng")]
        public required long? Mshkd { get; set; }
    }
}
