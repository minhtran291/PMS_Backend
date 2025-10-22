using AutoMapper;
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

                var rsqValidation = ValidateRequestSalesQuotation(rsq);
                if (rsqValidation != null)
                    return rsqValidation;

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
                    RsqId = rsq.Id,
                    RequestCode = rsq.RequestCode,
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

        public async Task<ServiceResult<object>> CreateSalesQuotationAsync(CreateSalesQuotationDTO dto)
        {
            try
            {
                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.RequestSalesQuotationDetails)
                    .FirstOrDefaultAsync(r => r.Id == dto.RsqId);

                var rsqValidation = ValidateRequestSalesQuotation(rsq);
                if (rsqValidation != null)
                    return rsqValidation;

                var validityValidation = await ValidateValidityAsync(dto.ValidityId);
                if (validityValidation != null)
                    return validityValidation;

                var lotsValidation = await ValidateLotsAsync(dto, rsq);
                if (lotsValidation != null)
                    return lotsValidation;

                var taxValidation = await ValidateTaxesAsync(dto);
                if (taxValidation != null)
                    return taxValidation;

                await _unitOfWork.BeginTransactionAsync();

                var salesQuotation = new Core.Domain.Entities.SalesQuotation
                {
                    RsqId = rsq.Id,
                    SqvId = dto.ValidityId,
                    QuotationCode = GenerateQuotationCode(),
                    Status = Core.Domain.Enums.SalesQuotationStatus.Draft,
                    SalesQuotaionDetails = dto.Details.Select(item => new SalesQuotaionDetails
                    {
                        LotId = item.LotId,
                        TaxId = item.TaxId,
                    }).ToList(),
                };

                await _unitOfWork.SalesQuotation.AddAsync(salesQuotation);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Tạo báo giá thất bại",
                };
            }
        }

        private static string GenerateQuotationCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"SQ-{datePart}-{randomPart}";
        }

        private static ServiceResult<object>? ValidateRequestSalesQuotation(Core.Domain.Entities.RequestSalesQuotation? rsq)
        {
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

            return null;
        }


        private async Task<ServiceResult<object>?> ValidateValidityAsync(int validityId)
        {
            var validity = await _unitOfWork.SalesQuotationValidity
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == validityId && v.Status == true);

            if (validity == null)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Thời hạn báo giá không hợp lệ hoặc đã bị vô hiệu"
                };

            return null;
        }

        private async Task<ServiceResult<object>?> ValidateLotsAsync(CreateSalesQuotationDTO dto, Core.Domain.Entities.RequestSalesQuotation rsq)
        {
            if (dto.Details == null || dto.Details.Count == 0)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách chi tiết báo giá không được để trống"
                };

            var validProductIds = rsq.RequestSalesQuotationDetails
                .Select(r => r.ProductId)
                .ToHashSet();

            var lotIds = dto.Details.Select(d => d.LotId).ToList();

            if (lotIds.Count != lotIds.Distinct().Count())
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách lô hàng có lô trùng lặp"
                };

            var lots = await _unitOfWork.LotProduct.Query()
                .Include(l => l.Product)
                .AsNoTracking()
                .Where(l => lotIds.Contains(l.LotID))
                .ToListAsync();

            if (lots.Count != lotIds.Count)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có lô hàng không tồn tại trong hệ thống"
                };

            var invalidLots = lots.Where(l => l.ExpiredDate <= DateTime.Now || l.LotQuantity <= 0).ToList();
            if (invalidLots.Count != 0)
            {
                var names = string.Join(", ", invalidLots.Select(l => l.Product.ProductName));
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = $"Các lô hàng sau đã hết hạn hoặc hết hàng: {names}"
                };
            }

            var outOfScopeLots = lots.Where(l => !validProductIds.Contains(l.ProductID)).ToList();
            if (outOfScopeLots.Count != 0)
            {
                var names = string.Join(", ", outOfScopeLots.Select(l => l.Product.ProductName));
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = $"Các lô hàng sau không thuộc phạm vi yêu cầu báo giá: {names}"
                };
            }

            return null;
        }


        private async Task<ServiceResult<object>?> ValidateTaxesAsync(CreateSalesQuotationDTO dto)
        {
            var taxIds = dto.Details.Select(d => d.TaxId).Distinct().ToList();

            var taxPolicies = await _unitOfWork.TaxPolicy.Query()
                .AsNoTracking()
                .Where(t => taxIds.Contains(t.Id) && t.Status == true)
                .ToListAsync();

            if (taxPolicies.Count != taxIds.Count)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có chính sách thuế không hợp lệ hoặc đã bị vô hiệu"
                };

            return null;
        }

    }
}
