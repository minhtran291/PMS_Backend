using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PRFQ
{
    public class CreatePRFQDTO
    {
        [Required(ErrorMessage = "SupplierId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "SupplierId phải lớn hơn 0.")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "TaxCode là bắt buộc.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "TaxCode phải có độ dài từ 5 đến 20 ký tự.")]
        public string TaxCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "MyPhone là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]
        public string MyPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "MyAddress là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự.")]
        public string MyAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh sách sản phẩm không được để trống.")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm.")]
        public List<int> ProductIds { get; set; } = new();

        public required PRFQStatus PRFQStatus { get; set; }
    }
}
