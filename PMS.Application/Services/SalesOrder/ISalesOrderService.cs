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
        //Sales Order Draft
        Task<ServiceResult<SalesQuotationResponseDTO>> GetQuotationInfo(int SalesQuotationId);
        Task<ServiceResult<object>> CreateDraftFromSalesQuotationAsync(int salesQuotationId, string createdBy);
        Task<ServiceResult<bool>> UpdateDraftQuantitiesAsync(int SalesOrderId, List<SalesOrderDetailsUpdateDTO> items);
        Task<ServiceResult<bool>> DeleteDraftAsync(string orderId);

        //Send Order and check current product quantity
        Task<ServiceResult<object>> SendOrderAsync(string orderId);

        //Customer mark is receipted of goods
        Task<ServiceResult<bool>> MarkCompleteAsync(string orderId);

        //Create payment
        Task<ServiceResult<VnPayInitResponseDTO>> GenerateVnPayPaymentAsync(string orderId, string paymentType);

        //Sale Staff confirm payment manual
        Task<ServiceResult<bool>> ConfirmPaymentAsync(string salesOrderId);

        //View Sales Orders
        Task<ServiceResult<object>> GetOrderDetailsAsync(string SalesOrderId);
        Task<ServiceResult<IEnumerable<object>>> ListOrdersAsync(string userId);
    }
}
