using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.RequestSalesQuotation
{
    public class RequestSalesQuotationService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RequestSalesQuotationService> logger) : Service(unitOfWork, mapper), IRequestSalesQuotationService
    {
        private readonly ILogger<RequestSalesQuotationService> _logger = logger;

        public async Task<ServiceResult<bool>> CreateRequestSalesQuotation(CreateRsqDTO dto, string? userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("Loi user id null");

                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                    ?? throw new Exception("Loi khong tim thay user id");

                var profile = user.CustomerProfile
                    ?? throw new Exception("Customer chua co profile");

                if (profile.Mst == null || profile.ImageCnkd == null || profile.ImageByt == null || profile.Mshkd == null)
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Bạn chưa đủ điều kiện để yêu cầu báo giá. Vui lòng cập nhật lại hồ sơ.",
                        Data = false
                    };

                await _unitOfWork.BeginTransactionAsync();

                var requestSalesQuotation = new Core.Domain.Entities.RequestSalesQuotation
                {
                    CustomerId = profile.Id,
                    RequestCode = GenerateRequestCode(),
                    RequestDate = DateTime.Now,
                    Status = Core.Domain.Enums.RequestSalesQuotationStatus.Draft
                };

                await _unitOfWork.RequestSalesQuotation.AddAsync(requestSalesQuotation);
                await _unitOfWork.CommitAsync();

                foreach (var item in dto.RsqDetails)
                {
                    var details = new RequestSalesQuotationDetails
                    {
                        RequestSalesQuotationId = requestSalesQuotation.Id,
                        ProductId = item.ProductId,
                    };

                    await _unitOfWork.RequestSalesQuotationDetails.AddAsync(details);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 201,
                    Message = "Tạo yêu cầu báo giá thành công",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Tạo yêu cầu báo giá thất bại",
                    Data = false
                };
            }
        }

        private static string GenerateRequestCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            return $"RSQ-{datePart}-{randomPart}";
        }

        public async Task<ServiceResult<List<ViewRsqDTO>>> ViewRequestSalesQuotationList(string? userId)
        {
            try
            {
                if(userId == null)
                    throw new Exception("Loi user id null");

                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId)
                    ?? throw new Exception("Loi khong tim thay user id");

                var profile = user.CustomerProfile
                    ?? throw new Exception("Customer chua co profile");

                var list = await _unitOfWork.RequestSalesQuotation.Query()
                    .Where(r => r.CustomerId == profile.Id)
                    .Select(r => new ViewRsqDTO
                    {
                        Id = r.Id,
                        RequestCode = r.RequestCode,
                        RequestDate = r.RequestDate,
                        Status = r.Status,
                    })
                    .ToListAsync();

                return new ServiceResult<List<ViewRsqDTO>>
                {
                    StatusCode = 200,
                    Message = list.Count == 0 ? "Chưa có yêu cầu báo giá nào" : "",
                    Data = list
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<List<ViewRsqDTO>>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<ViewRsqDTO>> ViewRequestSalesQuotationDetails(int rsqId)
        {
            try
            {
                var requestSalesQuotation = await _unitOfWork.RequestSalesQuotation.Query()
                    .FirstOrDefaultAsync(r => r.Id == rsqId)
                    ?? throw new Exception("Khong tim thay request sales quotation id");

                return new ServiceResult<ViewRsqDTO>
                {
                    StatusCode = 200,
                    Message = "",
                    Data = new ViewRsqDTO
                    {
                        Id = requestSalesQuotation.Id,
                        RequestCode = requestSalesQuotation.RequestCode,
                        RequestDate = requestSalesQuotation.RequestDate,
                        Status = requestSalesQuotation.Status,
                        Details = requestSalesQuotation.RequestSalesQuotationDetails.Select(d => new DTOs.RequestSalesQuotationDetails.ViewRsqDetailsDTO
                        {
                            ProductId = d.ProductId,
                            ProductName = d.Product.ProductName,
                        }).ToList()
                    }
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<ViewRsqDTO>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                    Data = null
                };
            }
        }
    }
}
