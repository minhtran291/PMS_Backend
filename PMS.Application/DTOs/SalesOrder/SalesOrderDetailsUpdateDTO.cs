using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class SalesOrderDetailsUpdateDTO
    {
        [Required(ErrorMessage = "ProductId là bắt buộc!")]
        public int ProductId { get; set; }
        public int LotId { get; set; }
        [Required(ErrorMessage = "Số lượng là bắt buộc!")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được là số âm!")]
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotalPrice { get; set; }
    }
}
