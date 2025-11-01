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
        //Get list product follow Quotation by quotationId
        Task<ServiceResult<List<QuotationProductDTO>>> GetQuotationProductsAsync(int qid);

        //Customer input quantity
        Task<ServiceResult<FEFOPlanResponseDTO>> BuildFefoPlanAsync(FEFOPlanRequestDTO request);

        //Make Pending Order
        Task<ServiceResult<CreateOrderResponseDTO>> CreateOrderFromQuotationAsync(
            FEFOPlanRequestDTO request, string createdBy);

        //Create payment
        Task<ServiceResult<VnPayInitResponseDTO>> GenerateVnPayPaymentAsync(string orderId, string paymentType);

        //Confirm payment
        Task<ServiceResult<bool>> ConfirmPaymentAsync(
            string salesOrderId, decimal amountVnd, string method, string? externalTxnId = null);
        //View OrderDetails
        Task<ServiceResult<object>> GetOrderDetailsAsync(string orderId);
    }
}
