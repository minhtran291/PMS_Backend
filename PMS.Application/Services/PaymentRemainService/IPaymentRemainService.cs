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
        Task<ServiceResult<PMS.Core.Domain.Entities.PaymentRemain>> 
            CreatePaymentRemainForGoodsIssueNoteAsync(int goodsIssueNoteId);

        Task<ServiceResult<List<PaymentRemainItemDTO>>>
            GetPaymentRemainsAsync(PaymentRemainListRequestDTO request);

        Task<ServiceResult<PaymentRemainItemDTO>>
            GetPaymentRemainDetailAsync(int id);

        Task<ServiceResult<List<int>>>
            GetPaymentRemainIdsBySalesOrderIdAsync(int salesOrderId);

    }
}
