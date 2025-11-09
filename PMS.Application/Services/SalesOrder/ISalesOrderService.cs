using PMS.Application.DTOs.SalesOrder;
using PMS.Application.DTOs.VnPay;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
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
        Task<ServiceResult<IEnumerable<object>>> ListCustomerSalesOrdersAsync(string userId);
        Task<ServiceResult<SalesQuotationResponseDTO>> GetQuotationInfo(int salesQuotationId);
        Task<ServiceResult<object>> CreateDraftFromSalesQuotationAsync
            (CreateOrderFromQuotationRequestDTO req);
        Task<ServiceResult<bool>> UpdateDraftQuantitiesAsync
            (int salesOrderId, List<SalesOrderDetailsUpdateDTO> items);
        Task<ServiceResult<bool>> DeleteDraftAsync(int orderId);

        //Send Order and check current product quantity
        Task<ServiceResult<object>> SendOrderAsync(int salesOrderId);

        //Customer mark is receipted of goods
        Task<ServiceResult<bool>> MarkCompleteAsync(int salesOrderId);

        //Create payment
        Task<ServiceResult<VnPayInitResponseDTO>> GenerateVnPayPaymentAsync
            (int salesOrderId, string paymentType);


        //View Sales Orders
        Task<ServiceResult<object>> GetOrderDetailsAsync(int salesOrderId);


        //Sales Staff
        Task<ServiceResult<IEnumerable<object>>> ListSalesOrdersAsync();
        Task<ServiceResult<bool>> ApproveSalesOrderAsync(int salesOrderId);
        Task<ServiceResult<bool>> RejectSalesOrderAsync(int salesOrderId);
        Task<ServiceResult<bool>> ConfirmPaymentAsync(int salesOrderId); // When Sales Order is approveed and cannot payment auto then manualy
    }
}
