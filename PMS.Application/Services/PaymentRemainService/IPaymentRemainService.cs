using PMS.Application.DTOs.PaymentRemain;
using PMS.Application.DTOs.VnPay;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.PaymentRemainService
{
    public interface IPaymentRemainService
    {
        // Tạo lệnh thanh toán cho 1 Invoice
        Task<ServiceResult<PaymentRemainItemDTO>>
            CreatePaymentRemainForInvoiceAsync(CreatePaymentRemainRequestDTO request);

        // Lấy list payment theo filter
        Task<ServiceResult<List<PaymentRemainItemDTO>>>
            GetPaymentRemainsAsync(PaymentRemainListRequestDTO request);

        // Lấy chi tiết 1 payment
        Task<ServiceResult<PaymentRemainItemDTO>>
            GetPaymentRemainDetailAsync(int id);

        // Lấy danh sách Id các payment (thành công) theo SalesOrder
        Task<ServiceResult<List<int>>>
            GetPaymentRemainIdsBySalesOrderIdAsync(int salesOrderId);

        // Đánh dấu 1 payment đã thanh toán thành công (dùng cho VNPay callback / kế toán xác nhận)
        Task<ServiceResult<bool>>
            MarkPaymentSuccessAsync(int paymentRemainId, string? gatewayTransactionRef = null);

        public Task<ServiceResult<VnPayInitResponseDTO>> 
            InitVnPayForInvoiceAsync(int invoiceId, decimal? amount, string clientIp, string? locale = "vn");

        //Tạo request check chuyển khoản ngân hàng
        public Task<ServiceResult<PaymentRemainItemDTO>> CreateBankTransferCheckRequestForInvoiceAsync(int invoiceId, CreateBankTransferCheckRequestDTO request);

        //kế toán chấp nhận là đã nhận được
        public Task<ServiceResult<bool>> ApproveBankTransferRequestAsync(int paymentRemainId, string accountantId);

        //Kế toán từ chối yêu cầu check kèm lý do
        public Task<ServiceResult<bool>> RejectBankTransferRequestAsync(int paymentRemainId, string reason, string accountantId);
    }
}
