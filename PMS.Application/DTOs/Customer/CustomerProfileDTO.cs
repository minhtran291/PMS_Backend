using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PMS.Application.DTOs.Customer
{
    public class CustomerProfileDTO
    {
        [Required(ErrorMessage = "Mã số thuế (Mst) là bắt buộc.")]
        [Range(1, long.MaxValue, ErrorMessage = "Mst phải lớn hơn 0.")]
        public long? Mst { get; set; }

        [Required(ErrorMessage = "Ảnh Chứng nhận Kinh doanh là bắt buộc.")]
        public IFormFile? ImageCnkd { get; set; }

        [Required(ErrorMessage = "Ảnh chứng nhận của bộ y tế là bắt buộc.")]
        public IFormFile? ImageByt { get; set; }

        [Required(ErrorMessage = "Mã số hộ kinh doanh là bắt buộc.")]
        [Range(1, long.MaxValue, ErrorMessage = "Mshkd phải lớn hơn 0.")]
        public long? Mshkd { get; set; }
    }
}
