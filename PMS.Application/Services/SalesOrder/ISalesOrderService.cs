using PMS.Application.DTOs.SalesOrder;
using PMS.Application.DTOs.VnPay;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.SalesOrder
{
    public interface ISalesOrderService
    {
        //Sales Order Draft (Customer)
        Task<ServiceResult<IEnumerable<SalesOrderItemDTO>>> ListCustomerSalesOrdersAsync(string userId);
        Task<ServiceResult<SalesQuotationResponseDTO>> GetQuotationInfo(int salesQuotationId);
        Task<ServiceResult<object>> CreateDraftFromSalesQuotationAsync
           (SalesOrderRequestDTO req);
        Task<ServiceResult<object>> UpdateDraftQuantitiesAsync
            (SalesOrderUpdateDTO upd);
        Task<ServiceResult<bool>> DeleteDraftAsync(int orderId);

        //Send Order and check current product quantity
        Task<ServiceResult<object>> SendOrderAsync(int salesOrderId);

        //Customer mark is receipted of goods
        Task<ServiceResult<bool>> MarkCompleteAsync(int salesOrderId);

        //View Sales Orders
        Task<ServiceResult<object>> GetOrderDetailsAsync(int salesOrderId);

        //Get total customer paid order to report
        Task<ServiceResult<bool>> RecalculateTotalReceiveAsync();

        //Sales Staff
        Task<ServiceResult<IEnumerable<SalesOrderItemDTO>>> ListSalesOrdersAsync();
        Task<ServiceResult<bool>> ApproveSalesOrderAsync(int salesOrderId, string salesStaffId);
        Task<ServiceResult<bool>> RejectSalesOrderAsync(RejectSalesOrderRequestDTO request, string salesStaffId);
        Task<ServiceResult<bool>> ConfirmPaymentAsync(int salesOrderId, PaymentStatus status); // When Sales Order is approveed and cannot payment auto then manualy


        //Cọc manual
        public Task<ServiceResult<bool>> CreateDepositCheckRequestAsync(CreateSalesOrderDepositCheckRequestDTO dto, string customerId);
        public Task<ServiceResult<bool>> ApproveDepositCheckAsync(int requestId, string accountantId);
        public Task<ServiceResult<bool>> RejectDepositCheckAsync(RejectSalesOrderDepositCheckDTO dto, string accountantId);
        public Task<ServiceResult<IEnumerable<SalesOrderDepositCheckItemDTO>>> ListDepositChecksForCustomerAsync(string customerId);
        public Task<ServiceResult<SalesOrderDepositCheckDetailDTO>> GetDepositCheckDetailForCustomerAsync(int requestId, string customerId);
        public Task<ServiceResult<bool>> UpdateDepositCheckRequestAsync(int requestId, string customerId, UpdateSalesOrderDepositCheckRequestDTO dto);
        public Task<ServiceResult<bool>> DeleteDepositCheckRequestAsync(int requestId, string customerId);

        //
        Task<ServiceResult<bool>> CheckAndUpdateDeliveredStatusAsync();

        //Lấy ra toàn bộ SalesOrder chưa lấy hết hàng
        Task<ServiceResult<IEnumerable<SalesOrderItemDTO>>> ListSaleOrderNotDeliveredAsync();
    }
}
