using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Application.DTOs.PO;

namespace PMS.Application.DTOs.PRFQ
{
    public class PurchaseOrderInputDto
    {
        [Required(ErrorMessage ="Thiếu excel Key")]
        public required string ExcelKey { get; set; } = string.Empty;
        public required List<PurchaseOrderDetailInput> Details { get; set; } = new List<PurchaseOrderDetailInput>();
        public required PMS.Core.Domain.Enums.PurchasingOrderStatus status;
    }
}
