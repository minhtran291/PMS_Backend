using Microsoft.AspNetCore.Mvc;
using PMS.Core.Domain.Entities;
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
        [Required(ErrorMessage = "SalesQuotationID là bắt buộc!")]
        public int SalesQuotationId { get; set; }
        public required string CreateBy { get; set; }
        //public DateTime CreateAt { get; set; } = DateTime.Now;
        //public SalesOrderStatus Status { get; set; }
        //public decimal TotalPrice { get; set; }
        //public bool IsDeposited { get; set; } = false;

        [Required, MinLength(1)]
        public List<SalesOrderDetailsRequestDTO> Details { get; set; } = [];
        //public CustomerDebt CustomerDebt { get; set; }
    }
}
