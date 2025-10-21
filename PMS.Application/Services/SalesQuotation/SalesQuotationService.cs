using AutoMapper;
using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.SalesQuotation
{
    public class SalesQuotationService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SalesQuotationService> logger) : Service(unitOfWork, mapper), ISalesQuotationService
    {
        private readonly ILogger<SalesQuotationService> _logger = logger;
        public async Task<ServiceResult<object>> GenerateFormAsync(int rsqId)
        {
            try
            {
                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                .Include(r => r.RequestSalesQuotationDetails)
                .FirstOrDefaultAsync(r => r.Id == rsqId);

                if (rsq == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy yêu cầu báo giá"
                    };

                if (rsq.Status != Core.Domain.Enums.RequestSalesQuotationStatus.Sent)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Không thể tạo báo giá cho yêu cầu báo giá chưa được gửi"
                    };

                var listRequest = rsq.RequestSalesQuotationDetails;

                List<LotProduct> listLot = [];

                foreach (var item in listRequest)
                {
                    var lotProduct = await _unitOfWork.LotProduct
                    .Query()
                    .Include(lp => lp.Product)
                    .Where(lp => lp.ProductID == item.ProductId)
                    .ToListAsync();

                    listLot.AddRange(lotProduct);
                }

                var result = _mapper.Map<List<FormSalesQuotationDTO>>(listLot);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Tạo form thất bại",
                };
            }
        }
    }
}
