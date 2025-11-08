using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class SalesQuotationDetailsDTO
    {
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(1000, double.MaxValue ,ErrorMessage = "Giá bản phải từ 1000 trở lên")]
        public decimal SalesPrice { get; set; }
        [Required(ErrorMessage = "Hạn dùng dự kiến không được để chống")]
        public required string ExpectedExpiryNote { get; set; }
        public int TaxId { get; set; }
        public string? Note { get; set; }
    }
}
