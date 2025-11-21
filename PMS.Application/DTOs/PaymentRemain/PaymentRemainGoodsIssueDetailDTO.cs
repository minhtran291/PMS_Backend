using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PaymentRemain
{
    public class PaymentRemainGoodsIssueDetailDTO
    {
        public int Index { get; set; }               // STT
        public string ProductName { get; set; } = ""; // Tên sản phẩm
        public int Quantity { get; set; }           // Số lượng

        public decimal UnitPrice { get; set; }      // Đơn giá (chưa thuế)
        public decimal TaxPercent { get; set; }     // Thuế (%)
        public decimal UnitPriceAfterTax { get; set; } // Đơn giá sau thuế

        public DateTime? ExpiredDate { get; set; }  // Ngày hết hạn

        public decimal SubTotal { get; set; }       // Tạm tính (chưa thuế)
        public decimal SubTotalAfterTax { get; set; } // Tạm tính sau thuế
    }
}
