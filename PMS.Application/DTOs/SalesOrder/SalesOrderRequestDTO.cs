using Microsoft.AspNetCore.Mvc;
using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderRequestDTO
    {
        public string SalesOrderCode { get; set; }
        [Required(ErrorMessage = "SalesQuotationID là bắt buộc!")]
        public int SalesQuotationId { get; set; }
        public required string CreateBy { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public SalesOrderStatus Status { get; set; }
        [Required(ErrorMessage = "Tổng giá trị đơn hàng là bắt buộc!")]
        public decimal TotalPrice { get; set; }
        public bool IsDeposited { get; set; } = false;

        [Required, MinLength(1)]
        public List<SalesOrderDetailsRequestDTO> Details { get; set; } = [];
    }
}
