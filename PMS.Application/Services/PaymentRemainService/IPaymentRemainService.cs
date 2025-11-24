using PMS.Application.DTOs.PaymentRemain;
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
    }
}
