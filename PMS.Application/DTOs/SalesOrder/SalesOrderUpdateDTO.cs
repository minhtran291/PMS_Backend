using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderUpdateDTO
    {
        [Required(ErrorMessage = "SalesOrderID là bắt buộc!")]
        public int SalesOrderId { get; set; }
        [Required(ErrorMessage = "Mã đơn hàng mua là bắt buộc!")]
        public string SalesOrderCode { get; set; }
        [Required(ErrorMessage = "SalesQuotationID là bắt buộc!")]
        public int SalesQuotationId { get; set; }
        [Required(ErrorMessage = "Người tạo đơn hàng phải được ghi vào hệ thống!")]
        public required string CreateBy { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public SalesOrderStatus Status { get; set; }
        [Required(ErrorMessage = "Tổng giá trị đơn hàng là bắt buộc!")]
        public decimal TotalPrice { get; set; }
        public bool IsDeposited { get; set; } = false;

        [Required, MinLength(1)]
        public List<SalesOrderDetailsUpdateDTO> Details { get; set; } = [];
    }
}
