using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderDetailsRequestDTO
    {
        [Required(ErrorMessage = "ProductId là bắt buộc!")]
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Số lượng là bắt buộc!")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng tối thiểu phải lớn hơn hoặc bằng 1")]
        public int Quantity { get; set; }
        [Required(ErrorMessage = "Đơn giá là bắt buộc!")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền không được là số âm!")]
        public decimal UnitPrice { get; set; }
        [Required(ErrorMessage = "Giá dòng là bắt buộc!")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền không được là số âm!")]
        public decimal SubTotalPrice { get; set; }
    }
}
