using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Application.Services.Base;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Identity;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.RequestSalesQuotation
{
    public class RequestSalesQuotationService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RequestSalesQuotationService> logger,
        INotificationService notificationService) : Service(unitOfWork, mapper), IRequestSalesQuotationService
    {
        private readonly ILogger<RequestSalesQuotationService> _logger = logger;
        private readonly INotificationService _notificationService = notificationService;

        public async Task<ServiceResult<object>> CreateRequestSalesQuotation(CreateRsqDTO dto, string? customerProfileId)
        {
            try
            {
                if (!int.TryParse(customerProfileId, out int profileId))
                {
                    throw new Exception("CustomerProfileId khong hop le");
                }

                var profile = await _unitOfWork.CustomerProfile.Query().FirstOrDefaultAsync(cp => cp.Id == profileId)
                    ?? throw new Exception("Customer chua co profile");

                if (profile.Mst == null || profile.ImageCnkd == null || profile.ImageByt == null || profile.Mshkd == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Bạn chưa đủ điều kiện để yêu cầu báo giá. Vui lòng cập nhật lại hồ sơ.",
                        Data = null
                    };

                if (dto.ProductIdList == null || dto.ProductIdList.Count == 0)
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Bạn phải chọn ít nhất một sản phẩm",
                        Data = null
                    };
                }

                var productIds = dto.ProductIdList;
                if (productIds.Count != productIds.Distinct().Count())
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Danh sách sản phẩm có sản phẩm bị trùng",
                        Data = null
                    };
                }

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

                foreach (var productId in productIds)
                {
                    var details = new RequestSalesQuotationDetails
                    {
                        RequestSalesQuotationId = requestSalesQuotation.Id,
                        ProductId = productId,
                    };

                    await _unitOfWork.RequestSalesQuotationDetails.AddAsync(details);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo yêu cầu báo giá thành công",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Tạo yêu cầu báo giá thất bại",
                    Data = null
                };
            }
        }

        private static string GenerateRequestCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            return $"RSQ-{datePart}-{randomPart}";
        }

        public async Task<ServiceResult<List<ViewRsqDTO>>> ViewRequestSalesQuotationList(string? customerProfileId)
        {
            try
            {
                if (!int.TryParse(customerProfileId, out int profileId))
                {
                    throw new Exception("CustomerProfileId khong hop le");
                }

                var profile = await _unitOfWork.CustomerProfile.Query().FirstOrDefaultAsync(cp => cp.Id == profileId)
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
            catch (Exception ex)
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

        public async Task<ServiceResult<object>> UpdateRequestSalesQuotation(UpdateRsqDTO dto, string? customerProfileId)
        {
            try
            {
                if (!int.TryParse(customerProfileId, out int profileId))
                {
                    throw new Exception("CustomerProfileId khong hop le");
                }

                var profile = await _unitOfWork.CustomerProfile.Query().FirstOrDefaultAsync(cp => cp.Id == profileId)
                    ?? throw new Exception("Customer chua co profile");

                if (profile.Mst == null || profile.ImageCnkd == null || profile.ImageByt == null || profile.Mshkd == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Bạn chưa đủ điều kiện để yêu cầu báo giá. Vui lòng cập nhật lại hồ sơ.",
                        Data = null
                    };

                if (dto.ProductIdList == null || dto.ProductIdList.Count == 0)
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Bạn phải chọn ít nhất một sản phẩm",
                        Data = null
                    };
                }

                var productIds = dto.ProductIdList;
                if (productIds.Count != productIds.Distinct().Count())
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Danh sách sản phẩm có sản phẩm bị trùng",
                        Data = null
                    };
                }

                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .FirstOrDefaultAsync(r => r.Id == dto.RsqId)
                    ?? throw new Exception("Loi khong tim thay request sales quotation id");

                if (rsq.Status == Core.Domain.Enums.RequestSalesQuotationStatus.Sent)
                    return new ServiceResult<object>
                    {
                        StatusCode = 200,
                        Message = "Yêu cầu báo giá đã gửi không thể cập nhật",
                        Data = null
                    };

                var existingDetails = rsq.RequestSalesQuotationDetails.ToList();

                var newDetails = dto.ProductIdList;

                var toAdd = newDetails
                    .Where(nd => !existingDetails.Any(ed => ed.ProductId == nd))
                    .Select(nd => new RequestSalesQuotationDetails
                    {
                        RequestSalesQuotationId = rsq.Id,
                        ProductId = nd,
                    })
                    .ToList();

                if (toAdd.Count != 0)
                    _unitOfWork.RequestSalesQuotationDetails.AddRange(toAdd);

                var toRemove = existingDetails
                    .Where(ed => !newDetails.Any(nd => nd == ed.ProductId))
                    .ToList();

                if (toRemove.Count != 0)
                    _unitOfWork.RequestSalesQuotationDetails.RemoveRange(toRemove);

                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Cập nhật thành công",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<object>> SendSalesQuotationRequest(string? customerProfileId, int rsqId)
        {
            try
            {
                if (!int.TryParse(customerProfileId, out int profileId))
                {
                    throw new Exception("CustomerProfileId khong hop le");
                }

                var profile = await _unitOfWork.CustomerProfile.Query().FirstOrDefaultAsync(cp => cp.Id == profileId)
                    ?? throw new Exception("Customer chua co profile");

                if (profile.Mst == null || profile.ImageCnkd == null || profile.ImageByt == null || profile.Mshkd == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Bạn chưa đủ điều kiện để yêu cầu báo giá. Vui lòng cập nhật lại hồ sơ.",
                        Data = null
                    };

                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .FirstOrDefaultAsync(r => r.Id == rsqId)
                    ?? throw new Exception("Khong tim thay request sales quotation id");

                rsq.Status = Core.Domain.Enums.RequestSalesQuotationStatus.Sent;

                _unitOfWork.RequestSalesQuotation.Update(rsq);
                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationToRolesAsync(
                    profile.User.Id,
                    ["SALES_STAFF"],
                    "Yêu cầu báo giá",
                    "Bạn nhận được 1 yêu cầu báo giá mới từ khách hàng",
                    Core.Domain.Enums.NotificationType.Message
                    );

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Đã gửi yêu cầu báo giá",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<object>> RemoveRequestSalesQuotation(int rsqId)
        {
            try
            {
                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.RequestSalesQuotationDetails)
                    .FirstOrDefaultAsync(r => r.Id == rsqId)
                    ?? throw new Exception("Khong tim thay request sales quotation id");

                if (rsq.Status == Core.Domain.Enums.RequestSalesQuotationStatus.Sent)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Báo giá đã gửi không thể xóa",
                        Data = null
                    };

                await _unitOfWork.BeginTransactionAsync();

                var details = rsq.RequestSalesQuotationDetails;

                _unitOfWork.RequestSalesQuotationDetails.RemoveRange(details);

                _unitOfWork.RequestSalesQuotation.Remove(rsq);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Đã xóa yêu cầu báo giá",
                    Data = null
                };
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                    Data = null
                };
            }
        }
    }
}
