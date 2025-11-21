using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PaymentRemain
{
    public class PaymentRemainItemDTO
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public int? GoodsIssueNoteId { get; set; }
        public PaymentType PaymentType { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? GatewayTransactionRef { get; set; }
        public string? Gateway { get; set; }
        public string? SalesOrderCode { get; set; }
        public decimal SalesOrderTotalPrice { get; set; }
        public decimal SalesOrderPaidAmount { get; set; }

        public string? CustomerId { get; set; }          // ID Khách hàng
        public string? CustomerName { get; set; }        // Tên khách hàng
        public DateTime? RequestCreatedAt { get; set; }  // Ngày tạo yêu cầu thanh toán
        public string? PaymentStatusText { get; set; }   // Trạng thái hiển thị cho FE

        public string? GoodsIssueNoteCode { get; set; }  // Mã phiếu xuất
        public DateTime? GoodsIssueNoteCreatedAt { get; set; }

        public decimal DepositAmount { get; set; }       // Tiền đã cọc
        public decimal DepositPercent { get; set; }      // % cọc 

        public decimal RemainingAmount { get; set; }

        public List<PaymentRemainGoodsIssueDetailDTO> GoodsIssueDetails { get; set; } = new();
    }
}