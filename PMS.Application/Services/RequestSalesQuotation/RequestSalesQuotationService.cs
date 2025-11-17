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
                var profile = await ValidateCustomerProfile(customerProfileId);

                var productIds = dto.ProductIdList;

                var checkList = await ValidateProductId(productIds);

                if (checkList != null) return checkList;

                await _unitOfWork.BeginTransactionAsync();

                var requestSalesQuotation = new Core.Domain.Entities.RequestSalesQuotation
                {
                    CustomerId = profile.Id,
                    RequestCode = GenerateRequestCode(),
                    RequestDate = dto.Status == 1 ? DateTime.Now : null,
                    Status = dto.Status == 1 ? Core.Domain.Enums.RequestSalesQuotationStatus.Sent : Core.Domain.Enums.RequestSalesQuotationStatus.Draft,
                    RequestSalesQuotationDetails = productIds.Select(id => new RequestSalesQuotationDetails
                    {
                        ProductId = id,
                    }).ToList()
                };

                await _unitOfWork.RequestSalesQuotation.AddAsync(requestSalesQuotation);

                await _unitOfWork.CommitAsync();

                if(dto.Status == 1)
                {
                    await _notificationService.SendNotificationToRolesAsync(
                    profile.UserId,
                    [UserRoles.SALES_STAFF],
                    "Bạn nhận được 1 thông báo mới",
                    "Yêu cầu báo giá",
                    Core.Domain.Enums.NotificationType.Message
                    );
                }

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo yêu cầu báo giá thành công",
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
                };
            }
        }

        private static string GenerateRequestCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"RSQ-{datePart}-{randomPart}";
        }

        public async Task<ServiceResult<List<ViewRsqDTO>>> ViewRequestSalesQuotationList(string userId)
        {
            try
            {
                var customer = await GetUserProifleAsync(userId);

                var query = _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.CustomerProfile)
                        .ThenInclude(c => c.User)
                    .AsQueryable();

                if (customer != null)
                {
                    query = query.Where(r => r.CustomerId == customer.Id);
                }
                else
                {
                    query = query.Where(r => r.Status != Core.Domain.Enums.RequestSalesQuotationStatus.Draft);
                }

                var list = await query.Select(r => new ViewRsqDTO
                {
                    Id = r.Id,
                    CustomerName = r.CustomerProfile.User.FullName ?? "",
                    RequestCode = r.RequestCode,
                    RequestDate = r.RequestDate,
                    Status = r.Status,
                }).ToListAsync();


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
                };
            }
        }

        public async Task<ServiceResult<object>> ViewRequestSalesQuotationDetails(int rsqId, string userId)
        {
            try
            {
                var customer = await GetUserProifleAsync(userId);

                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.RequestSalesQuotationDetails)
                        .ThenInclude(d => d.Product)
                    .Include(r => r.CustomerProfile.User)
                    .FirstOrDefaultAsync(r => r.Id == rsqId);

                var validateRsq = ValidateRsqDetails(rsq, customer);

                if (validateRsq != null) return validateRsq;

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "",
                    Data = new ViewRsqDTO
                    {
                        Id = rsq.Id,
                        CustomerName = rsq.CustomerProfile.User.FullName ?? "",
                        RequestCode = rsq.RequestCode,
                        RequestDate = rsq.RequestDate,
                        Status = rsq.Status,
                        Details = rsq.RequestSalesQuotationDetails.Select(d => new DTOs.RequestSalesQuotationDetails.ViewRsqDetailsDTO
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
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                };
            }
        }

        public async Task<ServiceResult<object>> UpdateRequestSalesQuotation(UpdateRsqDTO dto, string? customerProfileId)
        {
            try
            {
                var profile = await ValidateCustomerProfile(customerProfileId);

                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.RequestSalesQuotationDetails)
                    .FirstOrDefaultAsync(r => r.Id == dto.RsqId);

                var validateRsq = ValidateRsq(rsq, profile, "Yêu cầu báo giá đã gửi không thể sửa");

                if (validateRsq != null) return validateRsq;

                var newDetails = dto.ProductIdList;

                var checkList = await ValidateProductId(newDetails);

                if (checkList != null) return checkList;

                var existingDetails = rsq.RequestSalesQuotationDetails.ToList();

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
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                };
            }
        }

        public async Task<ServiceResult<object>> SendSalesQuotationRequest(string? customerProfileId, int rsqId)
        {
            try
            {
                var profile = await ValidateCustomerProfile(customerProfileId);

                var user = await _unitOfWork.Users.UserManager.FindByIdAsync(profile.UserId)
                    ?? throw new Exception("User id khong ton tai");

                if (profile.Mst == null || profile.ImageCnkd == null || profile.Mshkd == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Bạn chưa đủ điều kiện để yêu cầu báo giá. Vui lòng cập nhật lại hồ sơ.",
                    };

                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .FirstOrDefaultAsync(r => r.Id == rsqId);

                var validateRsq = ValidateRsq(rsq, profile, "Yêu cầu báo giá đã gửi");

                if (validateRsq != null) return validateRsq;

                rsq.RequestDate = DateTime.Now;
                rsq.Status = Core.Domain.Enums.RequestSalesQuotationStatus.Sent;

                _unitOfWork.RequestSalesQuotation.Update(rsq);
                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationToRolesAsync(
                    user.Id,
                    ["SALES_STAFF"],
                    "Yêu cầu báo giá",
                    "Bạn nhận được 1 yêu cầu báo giá mới từ khách hàng",
                    Core.Domain.Enums.NotificationType.Message
                    );

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Đã gửi yêu cầu báo giá",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                };
            }
        }

        public async Task<ServiceResult<object>> RemoveRequestSalesQuotation(int rsqId, string? customerProfileId)
        {
            try
            {
                var profile = await ValidateCustomerProfile(customerProfileId);

                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.RequestSalesQuotationDetails)
                    .FirstOrDefaultAsync(r => r.Id == rsqId);

                var validateRsq = ValidateRsq(rsq, profile, "Yêu cầu báo giá đã gửi không thể xóa");

                if (validateRsq != null) return validateRsq;

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
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                };
            }
        }

        private async Task<ServiceResult<object>?> ValidateProductId(List<int> list)
        {
            if (list == null || list.Count == 0)
            {
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Bạn phải chọn ít nhất một sản phẩm",
                };
            }

            if (list.Count != list.Distinct().Count())
            {
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách sản phẩm có sản phẩm bị trùng",
                };
            }

            var productsInDb = await _unitOfWork.Product.Query()
                .Where(p => list.Contains(p.ProductID))
                .ToListAsync();

            var invalidMessages = new List<string>();

            for (int i = 0; i < list.Count; i++)
            {
                var productId = list[i];
                var product = productsInDb.FirstOrDefault(p => p.ProductID == productId);

                if (product == null)
                {
                    invalidMessages.Add($"Sản phẩm số {i + 1} không tồn tại.");
                }
                else if (!product.Status)
                {
                    invalidMessages.Add($"Sản phẩm số {i + 1} không hoạt động.");
                }
            }


            if (invalidMessages.Count > 0)
            {
                string combinedMessage = string.Join(" ", invalidMessages);
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = combinedMessage
                };
            }

            return null;
        }

        private async Task<CustomerProfile> ValidateCustomerProfile(string? customerProfileId)
        {
            if (!int.TryParse(customerProfileId, out int profileId))
                throw new Exception("CustomerProfileId khong hop le");

            var profile = await _unitOfWork.CustomerProfile.Query().FirstOrDefaultAsync(cp => cp.Id == profileId)
                ?? throw new Exception("Customer chua co profile");

            return profile;
        }

        private static ServiceResult<object>? ValidateRsq(Core.Domain.Entities.RequestSalesQuotation? rsq, CustomerProfile profile, string message)
        {
            if (rsq == null)
                return new ServiceResult<object>
                {
                    StatusCode = 404,
                    Message = "Yêu cầu báo giá không tồn tại",
                };

            if (rsq.CustomerId != profile.Id)
                return new ServiceResult<object>
                {
                    StatusCode = 403,
                    Message = "Bạn không có quyền thao tác trên yêu cầu báo giá này",
                };

            if (rsq.Status != Core.Domain.Enums.RequestSalesQuotationStatus.Draft)
                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = message
                };

            return null;
        }

        private static ServiceResult<object>? ValidateRsqDetails(Core.Domain.Entities.RequestSalesQuotation? rsq, CustomerProfile? customerProfile)
        {
            if (rsq == null)
                return new ServiceResult<object>
                {
                    StatusCode = 404,
                    Message = "Yêu cầu báo giá không tồn tại",
                };

            if (customerProfile != null)
            {
                if (rsq.CustomerId != customerProfile.Id)
                    return new ServiceResult<object>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền xem yêu cầu báo giá này",
                    };
            }
            else
            {
                if (rsq.Status == Core.Domain.Enums.RequestSalesQuotationStatus.Draft)
                    return new ServiceResult<object>
                    {
                        StatusCode = 403,
                        Message = "Báo giá chưa được gửi không thể xem",
                    };
            }

            return null;
        }

        private async Task<CustomerProfile?> GetUserProifleAsync(string userId)
        {
            var user = await _unitOfWork.Users.Query().Include(u => u.CustomerProfile).FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new Exception("Khong tim thay user");

            var isCustomer = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.CUSTOMER);

            var isStaff = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.SALES_STAFF);

            if (!isCustomer && !isStaff)
                throw new Exception("Role khong hop le");

            CustomerProfile? customer = null;

            if (isCustomer)
                customer = user.CustomerProfile;

            if (customer == null && !isStaff)
                throw new Exception("Customer profile null");

            return customer;
        }
    }
}
