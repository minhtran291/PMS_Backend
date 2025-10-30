using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Customer
{
    public class CustomerAdditionalInfoDTO
    {
        [Required(ErrorMessage = "Mã số thuế không được để trống")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mã số thuế phải có đúng 10 chữ số")]
        [Display(Name = "Mã số thuế")]
        public required long Mst { get; set; }

        [Required(ErrorMessage = "Mã số kinh doanh không được để trống")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mã số kinh doanh phải có đúng 10 chữ số")]
        [Display(Name = "Mã số kinh doanh")]
        public required long Mshkd { get; set; }

        [Required(ErrorMessage = "Ảnh chứng nhận kinh doanh là bắt buộc")]
        [Display(Name = "Ảnh chứng nhận kinh doanh")]
        public required string ImageCnkd { get; set; }

        [Required(ErrorMessage = "Ảnh báo cáo thuế là bắt buộc")]
        [Display(Name = "Ảnh báo cáo thuế")]
        public required string ImageByt { get; set; }
    }
}
