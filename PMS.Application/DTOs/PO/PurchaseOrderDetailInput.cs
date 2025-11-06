using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class PurchaseOrderDetailInput
    {
        [Required(ErrorMessage = "STT là bắt buộc")]
        public required int STT { get; set; }
        [Required(ErrorMessage = "Số lượng sản phẩm là bắt buộc")]
        public required int Quantity { get; set; }

    }
}
