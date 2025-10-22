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

                var productIds = rsq.RequestSalesQuotationDetails
                    .Select(r => r.ProductId)
                    .ToList();

                var listLot = await _unitOfWork.LotProduct
                    .Query()
                    .Include(lp => lp.Product)
                    .AsNoTracking()
                    .Where(lp => productIds.Contains(lp.ProductID) && lp.ExpiredDate > DateTime.Now && lp.LotQuantity > 0)
                    .ToListAsync();

                var listTax = await _unitOfWork.TaxPolicy.Query()
                    .AsNoTracking()
                    .Where(tp => tp.Status == true)
                    .ToListAsync();

                var listExpired = await _unitOfWork.SalesQuotationValidity.Query()
                    .AsNoTracking()
                    .Where(sqv => sqv.Status == true)
                    .ToListAsync();

                var lotDtos = _mapper.Map<List<LotDTO>>(listLot);
                var taxDtos = _mapper.Map<List<TaxPolicyDTO>>(listTax);
                var validityDtos = _mapper.Map<List<SalesQuotationValidityDTO>>(listExpired);

                var form = new FormSalesQuotationDTO
                {
                    Validities = validityDtos,
                    Taxes = taxDtos,
                    LotProducts = lotDtos,
                };

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = form
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
