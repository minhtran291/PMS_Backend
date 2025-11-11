using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class CustomerDebtRequestDTO
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "CustomerId là bắt buộc!")]
        public int CustomerId { get; set; }
        [Required(ErrorMessage = "SalesOrderId là bắt buộc!")]
        public int SalesOrderId { get; set; }
        [Required(ErrorMessage = "DebtAmount là bắt buộc!")]
        public decimal DebtAmount { get; set; }
    }
}
