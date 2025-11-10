using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PO
{
    public class PurchaseOrderByQuotaionInputDto
    {
        [Required(ErrorMessage = "Thiếu mã Quotation (QID)")]
        public required int QID { get; set; }

        [Required(ErrorMessage = "Danh sách sản phẩm là bắt buộc")]
        public required List<PurchaseOrderDetailByQuotaionInputDto> Details { get; set; } = new();

        [Required(ErrorMessage = "Thiếu trạng thái đơn hàng")]
        public  PurchasingOrderStatus Status { get; set; }

    }
}
