using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class PurchaseOrderDetailByQuotaionInputDto
    {
        [Required(ErrorMessage = "Thiếu ProductID")]
        public required int ProductID { get; set; }

        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Thiếu số lượng")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải > 0")]
        public required int Quantity { get; set; }
    }
}
