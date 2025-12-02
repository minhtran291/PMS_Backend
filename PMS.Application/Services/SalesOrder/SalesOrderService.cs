using AutoMapper;
using Castle.Core.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Ocsp;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.DTOs.VnPay;
using PMS.Application.Services.Base;
using PMS.Application.Services.Notification;
using PMS.Application.Services.VNpay;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.SalesOrder
{
    public class SalesOrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SalesOrderService> logger,
        INotificationService notificationService,
        IVnPayService vnPayService) : Service(unitOfWork, mapper), ISalesOrderService
    {
        private readonly ILogger<SalesOrderService> _logger = logger;
        private readonly INotificationService _noti = notificationService;
        private readonly IVnPayService _vnPay = vnPayService;
        private readonly IMapper _mapper = mapper;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;


        public async Task<ServiceResult<bool>> ConfirmPaymentAsync(int salesOrderId, PaymentStatus status)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                    .Include(o => o.CustomerDebts)
                    .Include(o => o.SalesQuotation)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (order == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng",
                        Data = false
                    };
                }

                if (order.SalesOrderStatus != SalesOrderStatus.Approved )
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ xác nhận thanh toán cọc cho đơn ở trạng thái đã được chấp thuận.",
                        Data = false
                    };
                }

                decimal depositAmount = order.TotalPrice * decimal.Round(order.SalesQuotation.DepositPercent / 100, 2);

                if (status == PaymentStatus.Deposited)
                {
                    order.PaymentStatus = status;
                    order.IsDeposited = true;
                    order.PaidAmount = depositAmount;
                    order.CustomerDebts.DebtAmount = order.TotalPrice - depositAmount;
                    if (DateTime.Now > order.SalesOrderExpiredDate)
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.BadDebt;
                    }
                    else
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.NoDebt;
                    }
                }

                if (status == PaymentStatus.Paid)
                {
                    order.PaymentStatus = status;
                    order.IsDeposited = true;
                    order.PaidAmount = order.TotalPrice;
                    order.CustomerDebts.DebtAmount = 0;
                    if (DateTime.Now > order.SalesOrderExpiredDate)
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.BadDebt;
                    }
                    else
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.NoDebt;
                    }
                }


                _unitOfWork.SalesOrder.Update(order);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Xác nhận thanh toán thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ConfirmPaymentAsync");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Lỗi xác nhận thanh toán",
                    Data = false
                };
            }
        }

        public async Task<ServiceResult<object>> CreateDraftFromSalesQuotationAsync(SalesOrderRequestDTO req)
        {
            try
            {
                if (req == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Payload trống.",
                        Data = null
                    };

                if (string.IsNullOrWhiteSpace(req.CreateBy))
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "CreateBy là bắt buộc.",
                        Data = null
                    };

                if (req.Details == null || req.Details.Count == 0)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Danh sách Details trống.",
                        Data = null

                    };

                var sq = await _unitOfWork.SalesQuotation.Query()
                    .Include(q => q.SalesQuotaionDetails)
                        .ThenInclude(d => d.Product)
                    .Include(q => q.SalesQuotaionDetails)
                        .ThenInclude(d => d.LotProduct)
                    .Include(q => q.SalesQuotaionDetails)
                        .ThenInclude(d => d.TaxPolicy)
                    .FirstOrDefaultAsync(q => q.Id == req.SalesQuotationId);

                if (sq == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesQuotation.",
                        Data = null
                    };

                if (sq.ExpiredDate < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "SalesQuotation đã hết hạn. Không thể tạo đơn nháp.",
                        Data = null
                    };

                var sqDetailByKey = sq.SalesQuotaionDetails
                    .Where(d => d.LotId.HasValue)
                    .ToDictionary(d => d.LotId!.Value, d => d);

                foreach (var it in req.Details)
                {
                    if (it.Quantity < 0)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Quantity âm ở {it.LotId}.",
                            Data = null
                        };

                    if (!sqDetailByKey.ContainsKey(it.LotId))
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Dòng không thuộc báo giá: LotId={it.LotId}.",
                            Data = null
                        };
                }

                await _unitOfWork.BeginTransactionAsync();

                //Create Sales Order Draft
                var order = new Core.Domain.Entities.SalesOrder
                {
                    SalesQuotationId = sq.Id,
                    SalesOrderCode = GenerateSalesOrderCode(),
                    CreateBy = req.CreateBy.Trim(),
                    CreateAt = DateTime.Now,
                    SalesOrderStatus = SalesOrderStatus.Draft,
                    IsDeposited = false,
                    TotalPrice = 0m,
                    PaidAmount = 0m,
                    SalesOrderExpiredDate = DateTime.Today.AddDays(7)
                };

                await _unitOfWork.SalesOrder.AddAsync(order);
                await _unitOfWork.CommitAsync();

                //add order details
                var detailEntities = new List<SalesOrderDetails>();
                foreach (var it in req.Details)
                {
                    var sqd = sqDetailByKey[it.LotId];

                    var basePrice = sqd.LotProduct?.SalePrice ?? 0m;

                    var taxRate = sqd.TaxPolicy?.Rate ?? 0m; 

                    var serverUnitPrice = decimal.Round(basePrice * (1 + taxRate), 2);

                    var sub = (it.Quantity > 0) ? decimal.Round(serverUnitPrice * it.Quantity, 2) : 0m;

                    detailEntities.Add(new SalesOrderDetails
                    {
                        SalesOrderId = order.SalesOrderId,
                        LotId = it.LotId,
                        Quantity = it.Quantity,
                        UnitPrice = serverUnitPrice,
                        SubTotalPrice = sub
                    });
                }

                if (detailEntities.Count > 0)
                    await _unitOfWork.SalesOrderDetails.AddRangeAsync(detailEntities);

                //Sum
                order.TotalPrice = detailEntities.Sum(d => d.SubTotalPrice);

                _unitOfWork.SalesOrder.Update(order);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var data = new
                {
                    order.SalesOrderId,
                    order.SalesOrderCode,
                    order.SalesQuotationId,
                    order.CreateBy,
                    order.CreateAt,
                    order.SalesOrderStatus,
                    order.IsDeposited,
                    order.TotalPrice,
                    order.SalesOrderExpiredDate,
                    Details = detailEntities.Select(d => new
                    {
                        d.LotId,
                        d.Quantity,
                        d.UnitPrice,
                        d.SubTotalPrice
                    }).ToList()
                };

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo SalesOrder Draft từ SalesQuotation thành công.",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi CreateDraftFromSalesQuotationAsync({SalesQuotationId})", req?.SalesQuotationId);
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi tạo bản nháp đơn hàng.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<bool>> DeleteDraftAsync(int salesOrderId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (so == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesOrder",
                        Data = false
                    };
                }

                if (so.SalesOrderStatus != SalesOrderStatus.Draft)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được xoá đơn ở trạng thái Draft",
                        Data = false
                    };
                }

                await _unitOfWork.BeginTransactionAsync();

                if (so.SalesOrderDetails != null && so.SalesOrderDetails.Count > 0)
                    _unitOfWork.SalesOrderDetails.RemoveRange(so.SalesOrderDetails);

                _unitOfWork.SalesOrder.Remove(so);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Xoá bản nháp đơn hàng thành công",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi DeleteDraftAsync");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi xoá bản nháp đơn hàng",
                    Data = false
                };
            }
        }


        #region CheckDepositManual

        //Customer yêu cầu kiếm tra tài khoản hoặc đã nhận được tiền mặt chưa
        public async Task<ServiceResult<bool>> CreateDepositCheckRequestAsync(
            CreateSalesOrderDepositCheckRequestDTO dto,string customerId)
        {
            try
            {
                if (dto == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Payload trống.",
                        Data = false
                    };
                }

                if (dto.SalesOrderId <= 0)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "SalesOrderId không hợp lệ.",
                        Data = false
                    };
                }

                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == dto.SalesOrderId);

                if (order == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng.",
                        Data = false
                    };
                }

                if (!string.Equals(order.CreateBy, customerId, StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền tạo yêu cầu cho đơn hàng này.",
                        Data = false
                    };
                }

                if (order.SalesOrderStatus != SalesOrderStatus.Approved)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ tạo yêu cầu cọc cho đơn đã được chấp thuận.",
                        Data = false
                    };
                }

                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng đã được thanh toán đủ.",
                        Data = false
                    };
                }

                if (order.SalesQuotation == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng không có thông tin báo giá.",
                        Data = false
                    };
                }

                var depositPercent = order.SalesQuotation.DepositPercent;
                var fullDeposit = Math.Round(order.TotalPrice * depositPercent / 100m, 2);

                var requestedAmount = dto.RequestedAmount ?? fullDeposit;
                if (requestedAmount <= 0)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Số tiền yêu cầu kiểm tra không hợp lệ.",
                        Data = false
                    };
                }

                var entity = new SalesOrderDepositCheck
                {
                    SalesOrderId = order.SalesOrderId,
                    RequestedAmount = requestedAmount,
                    PaymentMethod = dto.PaymentMethod,
                    CustomerNote = dto.CustomerNote,
                    Status = DepositCheckStatus.Pending,
                    RequestedBy = customerId,
                    RequestedAt = DateTime.Now
                };

                await _unitOfWork.SalesOrderDepositCheck.AddAsync(entity);
                await _unitOfWork.CommitAsync();

                // Gửi noti cho ACCOUNTANT
                try
                {
                    await _noti.SendNotificationToRolesAsync(
                        customerId,
                        new List<string> { "ACCOUNTANT" },
                        "Yêu cầu kiểm tra cọc đơn hàng",
                        $"Đơn {order.SalesOrderCode} có yêu cầu kiểm tra cọc số tiền {requestedAmount:N0}.",
                        NotificationType.Message
                    );
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification ACCOUNTANT thất bại khi tạo DepositCheck cho order {OrderId}",
                        order.SalesOrderId);
                }

                return new ServiceResult<bool>
                {
                    StatusCode = 201,
                    Message = "Tạo yêu cầu kiểm tra cọc thành công. Vui lòng đợi phản hồi!",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi CreateDepositCheckRequestAsync");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi tạo yêu cầu kiểm tra cọc.",
                    Data = false
                };
            }
        }

        //Accountant chấp nhận yêu cầu check cọc
        public async Task<ServiceResult<bool>> ApproveDepositCheckAsync(int requestId, string accountantId)
        {
            try
            {
                var req = await _unitOfWork.SalesOrderDepositCheck.Query()
                    .Include(r => r.SalesOrder)
                    .ThenInclude(o => o.SalesQuotation)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (req == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy yêu cầu kiểm tra cọc.",
                        Data = false
                    };
                }

                if (req.Status != DepositCheckStatus.Pending)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Yêu cầu đã được xử lý trước đó.",
                        Data = false
                    };
                }

                // Xác nhận cọc cho SalesOrder này
                var confirmResult = await ConfirmPaymentAsync(req.SalesOrderId, PaymentStatus.Deposited);
                if (confirmResult.StatusCode != 200 || !confirmResult.Data)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = confirmResult.StatusCode,
                        Message = "Không thể xác nhận cọc cho đơn hàng: " + confirmResult.Message,
                        Data = false
                    };
                }

                req.Status = DepositCheckStatus.Approved;
                req.CheckedBy = accountantId;
                req.CheckedAt = DateTime.Now;

                _unitOfWork.SalesOrderDepositCheck.Update(req);
                await _unitOfWork.CommitAsync();

                // Noti cho khách
                try
                {
                    await _noti.SendNotificationToCustomerAsync(
                        accountantId,
                        req.SalesOrder.CreateBy,
                        "Đã xác nhận cọc cho đơn hàng",
                        $"Đơn {req.SalesOrder.SalesOrderCode} đã được xác nhận cọc.",
                        NotificationType.Message
                    );
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification Customer thất bại khi approve DepositCheck {RequestId}",
                        req.Id);
                }

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Đã phê duyệt yêu cầu kiểm tra cọc.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ApproveDepositCheckAsync({RequestId})", requestId);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi phê duyệt yêu cầu kiểm tra cọc.",
                    Data = false
                };
            }
        }

        //Accountant reject yêu cầu check cọc có lý do
        public async Task<ServiceResult<bool>> RejectDepositCheckAsync(
            RejectSalesOrderDepositCheckDTO dto, string accountantId)
        {
            try
            {
                var req = await _unitOfWork.SalesOrderDepositCheck.Query()
                    .Include(r => r.SalesOrder)
                    .FirstOrDefaultAsync(r => r.Id == dto.RequestId);

                if (req == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy yêu cầu kiểm tra cọc.",
                        Data = false
                    };
                }

                if (req.Status != DepositCheckStatus.Pending)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Yêu cầu đã được xử lý trước đó.",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(dto.Reason))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Vui lòng nhập lý do từ chối.",
                        Data = false
                    };
                }

                req.Status = DepositCheckStatus.Rejected;
                req.RejectReason = dto.Reason.Trim();
                req.CheckedBy = accountantId;
                req.CheckedAt = DateTime.Now;

                _unitOfWork.SalesOrderDepositCheck.Update(req);
                await _unitOfWork.CommitAsync();

                // Noti cho khách
                try
                {
                    await _noti.SendNotificationToCustomerAsync(
                        accountantId,
                        req.SalesOrder.CreateBy,
                        "Yêu cầu kiểm tra cọc bị từ chối",
                        $"Yêu cầu xác nhận cọc cho đơn {req.SalesOrder.SalesOrderCode} đã bị từ chối. Lý do: {req.RejectReason}.",
                        NotificationType.Message
                    );
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification Customer thất bại khi reject DepositCheck {RequestId}",
                        req.Id);
                }

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Đã từ chối yêu cầu kiểm tra cọc.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi RejectDepositCheckAsync({RequestId})", dto.RequestId);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi từ chối yêu cầu kiểm tra cọc.",
                    Data = false
                };
            }
        }

        //Customer request check deposit
        public async Task<ServiceResult<IEnumerable<SalesOrderDepositCheckItemDTO>>> ListDepositChecksForCustomerAsync(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return new ServiceResult<IEnumerable<SalesOrderDepositCheckItemDTO>>
                    {
                        StatusCode = 400,
                        Message = "Thiếu thông tin khách hàng.",
                        Data = null
                    };
                }

                var query = _unitOfWork.SalesOrderDepositCheck.Query()
                    .Include(x => x.SalesOrder)
                    .Where(x => x.RequestedBy == customerId)
                    .OrderByDescending(x => x.RequestedAt);

                var items = await query
                    .Select(x => new SalesOrderDepositCheckItemDTO
                    {
                        Id = x.Id,
                        SalesOrderId = x.SalesOrderId,
                        SalesOrderCode = x.SalesOrder.SalesOrderCode,
                        RequestedAmount = x.RequestedAmount,
                        PaymentMethod = x.PaymentMethod,
                        Status = x.Status,
                        RequestedAt = x.RequestedAt
                    })
                    .ToListAsync();

                return new ServiceResult<IEnumerable<SalesOrderDepositCheckItemDTO>>
                {
                    StatusCode = 200,
                    Message = "Lấy danh sách yêu cầu kiểm tra cọc thành công.",
                    Data = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ListDepositChecksForCustomerAsync({CustomerId})", customerId);
                return new ServiceResult<IEnumerable<SalesOrderDepositCheckItemDTO>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi lấy danh sách yêu cầu kiểm tra cọc.",
                    Data = null
                };
            }
        }

        //Customer xem chi tiết yêu cầu check cọc cho đơn hàng của mình
        public async Task<ServiceResult<SalesOrderDepositCheckDetailDTO>> GetDepositCheckDetailForCustomerAsync(int requestId, string customerId)
        {
            try
            {
                var entity = await _unitOfWork.SalesOrderDepositCheck.Query()
                    .Include(x => x.SalesOrder)
                    .FirstOrDefaultAsync(x => x.Id == requestId);

                if (entity == null)
                {
                    return new ServiceResult<SalesOrderDepositCheckDetailDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy yêu cầu kiểm tra cọc.",
                        Data = null
                    };
                }

                if (!string.Equals(entity.RequestedBy, customerId, StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceResult<SalesOrderDepositCheckDetailDTO>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền xem yêu cầu này.",
                        Data = null
                    };
                }

                var dto = new SalesOrderDepositCheckDetailDTO
                {
                    Id = entity.Id,
                    SalesOrderId = entity.SalesOrderId,
                    SalesOrderCode = entity.SalesOrder.SalesOrderCode,
                    TotalOrderAmount = entity.SalesOrder.TotalPrice,
                    RequestedAmount = entity.RequestedAmount,
                    PaymentMethod = entity.PaymentMethod,
                    CustomerNote = entity.CustomerNote,
                    Status = entity.Status,
                    RequestedBy = entity.RequestedBy,
                    RequestedAt = entity.RequestedAt,
                    CheckedBy = entity.CheckedBy,
                    CheckedAt = entity.CheckedAt,
                    RejectReason = entity.RejectReason
                };

                return new ServiceResult<SalesOrderDepositCheckDetailDTO>
                {
                    StatusCode = 200,
                    Message = "Lấy chi tiết yêu cầu kiểm tra cọc thành công.",
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetDepositCheckDetailForCustomerAsync({RequestId})", requestId);
                return new ServiceResult<SalesOrderDepositCheckDetailDTO>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi lấy chi tiết yêu cầu kiểm tra cọc.",
                    Data = null
                };
            }
        }

        // Customer có thể sửa khi yêu cầu kiểm tra còn ở trạng thái pending
        public async Task<ServiceResult<bool>> UpdateDepositCheckRequestAsync(int requestId, string customerId, UpdateSalesOrderDepositCheckRequestDTO dto)
        {
            try
            {
                var entity = await _unitOfWork.SalesOrderDepositCheck.Query()
                    .Include(x => x.SalesOrder)
                        .ThenInclude(o => o.SalesQuotation)
                    .FirstOrDefaultAsync(x => x.Id == requestId);

                if (entity == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy yêu cầu kiểm tra cọc.",
                        Data = false
                    };
                }

                if (!string.Equals(entity.RequestedBy, customerId, StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền sửa yêu cầu này.",
                        Data = false
                    };
                }

                if (entity.Status != DepositCheckStatus.Pending)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được sửa yêu cầu ở trạng thái Pending.",
                        Data = false
                    };
                }

                if (entity.SalesOrder == null || entity.SalesOrder.SalesQuotation == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng không có thông tin báo giá.",
                        Data = false
                    };
                }

                var depositPercent = entity.SalesOrder.SalesQuotation.DepositPercent;
                var fullDeposit = Math.Round(entity.SalesOrder.TotalPrice * depositPercent / 100m, 2);

                var newRequestedAmount = dto.RequestedAmount ?? fullDeposit;
                if (newRequestedAmount <= 0)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Số tiền yêu cầu kiểm tra không hợp lệ.",
                        Data = false
                    };
                }

                entity.RequestedAmount = newRequestedAmount;
                entity.PaymentMethod = dto.PaymentMethod;
                entity.CustomerNote = dto.CustomerNote;

                _unitOfWork.SalesOrderDepositCheck.Update(entity);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Cập nhật yêu cầu kiểm tra cọc thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi UpdateDepositCheckRequestAsync({RequestId})", requestId);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi cập nhật yêu cầu kiểm tra cọc.",
                    Data = false
                };
            }
        }

        //Customer có thẻe xóa yêu cầu kiểm tra cọc khi yêu cầu còn ở trạng thái pending
        public async Task<ServiceResult<bool>> DeleteDepositCheckRequestAsync(int requestId, string customerId)
        {
            try
            {
                var entity = await _unitOfWork.SalesOrderDepositCheck.Query()
                    .FirstOrDefaultAsync(x => x.Id == requestId);

                if (entity == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy yêu cầu kiểm tra cọc.",
                        Data = false
                    };
                }

                if (!string.Equals(entity.RequestedBy, customerId, StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền xoá yêu cầu này.",
                        Data = false
                    };
                }

                if (entity.Status != DepositCheckStatus.Pending)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được xoá yêu cầu ở trạng thái Pending.",
                        Data = false
                    };
                }

                _unitOfWork.SalesOrderDepositCheck.Remove(entity);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Xoá yêu cầu kiểm tra cọc thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi DeleteDepositCheckRequestAsync({RequestId})", requestId);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi xoá yêu cầu kiểm tra cọc.",
                    Data = false
                };
            }
        }


        #endregion

        public async Task<ServiceResult<object>> GetOrderDetailsAsync(int salesOrderId)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(d => d.LotProduct)
                    .Include(o => o.SalesQuotation)
                        .ThenInclude(q => q.SalesQuotaionDetails)
                            .ThenInclude(qd => qd.TaxPolicy)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (order == null)
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng",
                        Data = null
                    };
                }

                var orderDetailsDto = order.SalesOrderDetails.Select(d => new
                {
                    d.SalesOrderId,
                    d.LotId,
                    ProductName = d.LotProduct.Product.ProductName,
                    d.Quantity,
                    d.UnitPrice,
                    d.SubTotalPrice,
                    Lot = d.LotProduct == null ? null : new
                    {
                        d.LotProduct.InputDate,
                        d.LotProduct.ExpiredDate
                    }
                }).ToList();

                var data = new
                {
                    order.SalesOrderId,
                    order.SalesOrderCode,
                    order.SalesQuotationId,
                    order.CreateBy,
                    order.CreateAt,
                    order.SalesOrderStatus,
                    order.PaymentStatus,
                    CustomerName = order.Customer.FullName,
                    DepositPercent = order.SalesQuotation.DepositPercent,
                    DepositAmount = order.TotalPrice * order.SalesQuotation.DepositPercent,
                    DepositExpiredDay = order.CreateAt.AddDays(order.SalesQuotation.DepositDueDays),
                    order.TotalPrice,
                    order.PaidAmount,
                    order.PaidFullAt,
                    order.RejectReason,
                    order.RejectedBy,
                    order.RejectedAt,
                    order.SalesOrderExpiredDate,
                    Details = orderDetailsDto
                };

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Lấy chi tiết đơn hàng thành công",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết đơn hàng");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi lấy chi tiết đơn hàng",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<SalesOrderItemDTO>>> ListCustomerSalesOrdersAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return new ServiceResult<IEnumerable<SalesOrderItemDTO>>
                    {
                        StatusCode = 400,
                        Message = "Thiếu userId.",
                        Data = null
                    };
                }

                var orders = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                    .Include(o => o.Customer)
                    .AsNoTracking()
                    .Where(o => o.CreateBy == userId)
                    .OrderByDescending(o => o.CreateAt)
                    .Select(o => new SalesOrderItemDTO
                    {
                        SalesOrderId = o.SalesOrderId,
                        SalesOrderCode = o.SalesOrderCode,
                        CustomerName = o.Customer.FullName,
                        SalesOrderStatus = o.SalesOrderStatus,
                        PaymentStatus = o.PaymentStatus,
                        SalesOrderStatusName = o.SalesOrderStatus.ToString(),
                        PaymentStatusName = o.PaymentStatus.ToString(),
                        IsDeposited = o.IsDeposited,
                        PaidFullAt = o.PaidFullAt,
                        PaidAmount = o.PaidAmount,
                        RejectReason = o.RejectReason,
                        RejectedAt = o.RejectedAt,
                        RejectBy = o.RejectedBy,
                        TotalPrice = o.TotalPrice,
                        CreateAt = o.CreateAt
                    })
                    .ToListAsync();

                return new ServiceResult<IEnumerable<SalesOrderItemDTO>>
                {
                    StatusCode = 200,
                    Message = "Lấy danh sách đơn hàng thành công.",
                    Data = orders
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ListCustomerSalesOrdersAsync({UserId})", userId);
                return new ServiceResult<IEnumerable<SalesOrderItemDTO>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi lấy danh sách đơn hàng.",
                    Data = null
                };
            }
        }

        //Customer mark order is complete
        public async Task<ServiceResult<bool>> MarkCompleteAsync(int salesOrderId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (so == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesOrder",
                        Data = false
                    };
                }

                if (so.PaymentStatus != PaymentStatus.Paid && so.SalesOrderStatus != SalesOrderStatus.Delivered)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được hoàn tất đơn khi đã thanh toán và trạng thái đơn hàng là đã giao hàng",
                        Data = false
                    };
                }

                so.SalesOrderStatus = SalesOrderStatus.Complete;
                _unitOfWork.SalesOrder.Update(so);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Đơn hàng đã được đánh dấu hoàn tất",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi MarkCompleteAsync");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi hoàn tất đơn hàng",
                    Data = false
                };
            }
        }

        //Customer send the sales order draft
        public async Task<ServiceResult<object>> SendOrderAsync(int salesOrderId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(l => l.LotProduct)
                            .ThenInclude(p => p.Product)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (so == null)
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesOrder",
                        Data = null
                    };
                }

                if (so.SalesOrderStatus != SalesOrderStatus.Draft)
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được gửi đơn ở trạng thái Draft",
                        Data = null
                    };
                }

                var warnings = new List<string>();
                foreach (var d in so.SalesOrderDetails)
                {
                    var lot = d.LotProduct;
                    if (lot == null) continue;

                    var totalAvailable = lot.LotQuantity;

                    if (d.Quantity > totalAvailable)
                    {
                        var prodName = lot.Product?.ProductName ?? $"Lô {d.LotId}";
                        var missing = d.Quantity - totalAvailable;
                        warnings.Add($"{prodName} (Lô {d.LotId}): thiếu {missing}");
                    }
                }

                if (warnings.Count > 0)
                {
                    var msg = string.Join("; ", warnings);
                    await _noti.SendNotificationToRolesAsync(
                        so.CreateBy,
                        new List<string> { "PURCHASES_STAFF" },
                        "Thiếu hàng khi khách gửi SalesOrder",
                        $"Các mặt hàng thiếu/sắp hết: {msg}",
                        NotificationType.Warning
                    );
                    await _noti.SendNotificationToRolesAsync(
                        so.CreateBy,
                        new List<string> { "SALES_STAFF" },
                        "Thiếu hàng khi khách gửi SalesOrder",
                        $"Liên hệ nhân viên mua hàng để có thể ra quyết định chấp nhận hoặc từ chối",
                        NotificationType.Message
                    );
                }

                
                await _unitOfWork.CommitAsync();

                so.SalesOrderStatus = SalesOrderStatus.Send;
                _unitOfWork.SalesOrder.Update(so);

                if (so.CustomerDebts == null)
                {
                    var debt = new PMS.Core.Domain.Entities.CustomerDebt
                    {
                        CustomerId = so.CreateBy, 
                        SalesOrderId = so.SalesOrderId,
                        DebtAmount = so.TotalPrice - so.PaidAmount,
                        status = CustomerDebtStatus.UnPaid 
                    };

                    await _unitOfWork.CustomerDebt.AddAsync(debt);
                    so.CustomerDebts = debt;
                }

                await _unitOfWork.CommitAsync();

                await _noti.SendNotificationToRolesAsync(
                    so.CreateBy,
                    new List<string> { "SALES_STAFF" },
                    "Có đơn hàng mới từ khách hàng",
                    $"Đơn hàng {so.SalesOrderCode} vừa được khách hàng gửi, vui lòng kiểm tra và xử lý.",
                    NotificationType.Message
                );

                await _unitOfWork.CommitTransactionAsync();

                var data = new
                {
                    so.SalesOrderId,
                    so.SalesOrderStatus,
                    so.TotalPrice,
                    CustomerDebt = so.CustomerDebts == null
                    ? null
                    : new
                    {
                        so.CustomerDebts.CustomerId,
                        so.CustomerDebts.SalesOrderId,
                        so.CustomerDebts.DebtAmount,
                        so.CustomerDebts.status
                    },
                    Warnings = warnings
                };

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = warnings.Count == 0
                        ? "Gửi đơn thành công"
                        : "Gửi đơn thành công, có mặt hàng thiếu/sắp hết",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi SendOrderAsync");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi gửi đơn",
                    Data = null
                };
            }
        }

        //customer update sales order when status is draft
        public async Task<ServiceResult<object>> UpdateDraftQuantitiesAsync(SalesOrderUpdateDTO upd)
        {
            try
            {
                // Validate payload
                if (upd == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Payload trống.",
                        Data = null
                    };

                if (upd.SalesOrderId <= 0)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "SalesOrderId là bắt buộc.",
                        Data = null
                    };

                if (upd.Details == null || upd.Details.Count == 0)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Danh sách Details trống.",
                        Data = null
                    };

                // Kiểm tra quantity âm sớm để fail fast
                foreach (var it in upd.Details)
                {
                    if (it.Quantity < 0)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Quantity âm ở ProductId={it.ProductId}.",
                            Data = null
                        };
                }

                // Lấy SalesOrder + Details + Debt
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == upd.SalesOrderId);

                if (order == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesOrder.",
                        Data = null
                    };

                if (order.SalesOrderStatus != SalesOrderStatus.Draft)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được phép sửa đơn hàng ở trạng thái Draft.",
                        Data = null
                    };

                // SalesOrder expired => cannot update
                if (order.SalesOrderExpiredDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "SalesOrder Draft đã hết hạn. Không thể cập nhật số lượng.",
                        Data = null
                    };

                var orderDetailByKey = order.SalesOrderDetails;
                    //.ToDictionary(d => (d.ProductId, d.LotId), d => d);

                foreach (var it in upd.Details)
                {
                    var key = (it.ProductId, it.LotId);
                    //if (!orderDetailByKey.ContainsKey(key))
                    //{
                    //    return new ServiceResult<object>
                    //    {
                    //        StatusCode = 400,
                    //        Message = $"Dòng không thuộc đơn hàng: ProductId={it.ProductId}, LotId={it.LotId}.",
                    //        Data = null
                    //    };
                    //}
                }

                await _unitOfWork.BeginTransactionAsync();

                foreach (var it in upd.Details)
                {
                    //var d = orderDetailByKey[(it.ProductId, it.LotId)];
                    //d.Quantity = it.Quantity;

                    //var sub = (it.Quantity > 0) ? decimal.Round(d.UnitPrice * it.Quantity, 2) : 0m;
                    //d.SubTotalPrice = sub;

                    //_unitOfWork.SalesOrderDetails.Update(d);
                }

                order.TotalPrice = order.SalesOrderDetails.Sum(x => x.SubTotalPrice);
                

                _unitOfWork.SalesOrder.Update(order);

                var debt = await _unitOfWork.CustomerDebt.Query()
                    .FirstOrDefaultAsync(x => x.SalesOrderId == order.SalesOrderId);

                if (debt != null)
                {
                    debt.DebtAmount = order.TotalPrice - order.PaidAmount;
                    _unitOfWork.CustomerDebt.Update(debt);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var response = new
                {
                    order.SalesOrderId,
                    order.SalesOrderCode,
                    order.SalesQuotationId,
                    order.SalesOrderStatus,
                    order.IsDeposited,
                    order.TotalPrice,
                    order.PaidAmount,
                    order.SalesOrderExpiredDate,
                    order.CreateBy,
                    order.CreateAt,
                    CustomerDebt = debt == null ? null : new
                    {
                        debt.CustomerId,
                        debt.SalesOrderId,
                        debt.DebtAmount,
                        debt.status
                    },
                    Details = order.SalesOrderDetails.Select(d => new
                    {
                        //d.ProductId,
                        d.LotId,
                        d.Quantity,
                        d.UnitPrice,
                        d.SubTotalPrice
                    }).ToList()
                };

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Cập nhật số lượng cho SalesOrder Draft thành công.",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi UpdateDraftQuantitiesAsync({SalesOrderId})", upd?.SalesOrderId);

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi cập nhật số lượng đơn nháp.",
                    Data = null
                };
            }
        }

        //Generate SalesOrderCode
        private static string GenerateSalesOrderCode()
            => $"SO{DateTime.Now:yyyyMMddHHmmssfff}";

        public async Task<ServiceResult<SalesQuotationResponseDTO>> GetQuotationInfo(int salesQuotationId)
        {
            try
            {
                var dto = await _unitOfWork.SalesQuotation.Query()
                    .Where(q => q.Id == salesQuotationId)
                    .Select(q => new SalesQuotationResponseDTO
                    {
                        Id = q.Id,
                        QuotationCode = q.QuotationCode,
                        QuotationDate = q.QuotationDate,
                        ExpiredDate = q.ExpiredDate,
                        Status = q.Status,
                        DepositPercent = q.DepositPercent,
                        DepositDueDays = q.DepositDueDays,
                        RsqId = q.RsqId,
                        SsId = q.SsId,
                        SqnId = q.SqnId,
                        Notes = q.Notes,

                        Details = q.SalesQuotaionDetails
                            .Select(d => new SalesQuotationDetailsResponseDTO
                            {
                                SalesQuotationDetailsId = d.Id,
                                ProductId = d.ProductId,
                                ProductName = d.Product.ProductName,
                                ProductUnit = d.Product.Unit,
                                ProductDescription = d.Product.ProductDescription,

                                LotId = d.LotId,
                                UnitPrice = d.LotProduct != null ? (decimal?)d.LotProduct.SalePrice : null,
                                LotInputDate = d.LotProduct != null ? (DateTime?)d.LotProduct.InputDate : null,
                                LotExpiredDate = d.LotProduct != null ? (DateTime?)d.LotProduct.ExpiredDate : null,

                                TaxId = d.TaxId,
                                Note = d.Note
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (dto == null)
                {
                    return new ServiceResult<SalesQuotationResponseDTO>
                    {
                        StatusCode = 404,
                        Message = $"Không tìm thấy SalesQuotation Id = {salesQuotationId}",
                        Data = null
                    };
                }

                return new ServiceResult<SalesQuotationResponseDTO>
                {
                    StatusCode = 200,
                    Message = "OK",
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetQuotationInfo({SalesQuotationId})", salesQuotationId);
                return new ServiceResult<SalesQuotationResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Đã xảy ra lỗi khi lấy thông tin báo giá.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<SalesOrderItemDTO>>> ListSalesOrdersAsync()
        {
            try
            {
                var orders = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.Customer)
                    .AsNoTracking()
                    .OrderByDescending(o => o.CreateAt)
                    .Select(o => new SalesOrderItemDTO
                    {
                        SalesOrderId = o.SalesOrderId,
                        SalesOrderCode = o.SalesOrderCode,
                        CustomerName = o.Customer.FullName,
                        SalesOrderStatus = o.SalesOrderStatus,
                        PaymentStatus = o.PaymentStatus,
                        SalesOrderStatusName = o.SalesOrderStatus.ToString(),
                        PaymentStatusName = o.PaymentStatus.ToString(),
                        IsDeposited = o.IsDeposited,
                        PaidAmount = o.PaidAmount,
                        PaidFullAt = o.PaidFullAt,
                        TotalPrice = o.TotalPrice,
                        RejectReason = o.RejectReason,
                        RejectedAt = o.RejectedAt,
                        RejectBy = o.RejectedBy,
                        CreateAt = o.CreateAt
                    })
                    .ToListAsync();

                return new ServiceResult<IEnumerable<SalesOrderItemDTO>>
                {
                    StatusCode = 200,
                    Message = "Lấy danh sách đơn hàng thành công.",
                    Data = orders
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ListSalesOrdersAsync");
                return new ServiceResult<IEnumerable<SalesOrderItemDTO>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi lấy danh sách đơn hàng.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<bool>> ApproveSalesOrderAsync(int salesOrderId, string salesStaffId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (so == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesOrder",
                        Data = false
                    };
                }

                if (so.SalesOrderStatus != SalesOrderStatus.Send)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi!",
                        Data = false
                    };
                }

                so.SalesOrderStatus = SalesOrderStatus.Approved;
                _unitOfWork.SalesOrder.Update(so);
                await _unitOfWork.CommitAsync();

                try
                {
                    await _noti.SendNotificationToCustomerAsync(
                       senderId: salesStaffId, 
                       receiverId: so.CreateBy,
                       title: "Đơn hàng đã được chấp thuận",
                       message: $"Đơn hàng {so.SalesOrderCode} của bạn đã được chấp thuận.",
                       type: NotificationType.Message
                    );
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification thất bại: orderId={OrderId}, receiver={ReceiverId}",
                        so.SalesOrderId, so.CreateBy);
                }

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Đơn hàng đã được chấp nhận!",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ApproveSalesOrderAsync");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi hoàn tất đơn hàng",
                    Data = false
                };
            }
        }

        public async Task<ServiceResult<bool>> RejectSalesOrderAsync(RejectSalesOrderRequestDTO request, string salesStaffId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .FirstOrDefaultAsync(o => o.SalesOrderId == request.SalesOrderId);

                if (so == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesOrder",
                        Data = false
                    };
                }

                if (so.SalesOrderStatus != SalesOrderStatus.Send)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi!",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Vui lòng nhập lý do từ chối đơn hàng",
                        Data = false
                    };
                }

                so.SalesOrderStatus = SalesOrderStatus.Rejected;
                so.RejectReason = request.Reason.Trim();
                so.RejectedAt = DateTime.Now;
                so.RejectedBy = salesStaffId;
                so.CustomerDebts.status = CustomerDebtStatus.Disable;
                so.CustomerDebts.DebtAmount = 0;

                _unitOfWork.SalesOrder.Update(so);
                await _unitOfWork.CommitAsync();

                try
                {
                    var title = "Đơn hàng đã bị từ chối";
                    var message = $"Đơn {so.SalesOrderCode} của bạn đã bị từ chối." +
                        $"Lý do: {so.RejectReason}.";

                    var senderId = salesStaffId;
                    var receiverId = so.CreateBy;

                    await _noti.SendNotificationToCustomerAsync(
                        senderId,
                        receiverId,
                        title,
                        message,
                        NotificationType.Message
                    );
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification thất bại: orderId={OrderId}, receiver={ReceiverId}",
                        so.SalesOrderId, so.CreateBy);
                }

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Đơn hàng đã bị từ chối!",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ApproveSalesOrderAsync");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi hoàn tất đơn hàng",
                    Data = false
                };
            }
        }

        public async Task<ServiceResult<bool>> RecalculateTotalReceiveAsync()
        {
            try
            {
                // 1) Tổng PaidAmount của tất cả SalesOrder
                var totalPaidFromOrders = await _unitOfWork.SalesOrder.Query()
                    .SumAsync(o => (decimal?)o.PaidAmount) ?? 0m;

                // 2) Lấy bản ghi PharmacySecretInfor (PMSID = 1)
                var info = await _unitOfWork.PharmacySecretInfor.Query()
                    .FirstOrDefaultAsync(x => x.PMSID == 1);

                if (info == null)
                {
                    // Nếu chưa có thì tạo mới
                    info = new PharmacySecretInfor
                    {
                        PMSID = 1,
                        Equity = 0m,
                        TotalRecieve = totalPaidFromOrders,
                        TotalPaid = 0m
                    };

                    await _unitOfWork.PharmacySecretInfor.AddAsync(info);
                }
                else
                {
                    // Nếu đã có thì cập nhật
                    info.TotalRecieve = totalPaidFromOrders;
                    _unitOfWork.PharmacySecretInfor.Update(info);
                }

                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Cập nhật TotalRecieve từ PaidAmount của SalesOrder thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi RecalculateTotalReceiveAsync khi tính TotalRecieve từ SalesOrder.PaidAmount");

                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi cập nhật TotalRecieve.",
                    Data = false
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<SalesOrderItemDTO>>> ListSaleOrderNotDeliveredAsync()
        {
            try
            {
                var orders = await _unitOfWork.SalesOrder.Query()
                    .Where(s => s.SalesOrderStatus == SalesOrderStatus.Approved && s.IsDeposited == true)
                    .AsNoTracking()
                    .OrderByDescending(o => o.CreateAt)
                    .Select(o => new SalesOrderItemDTO
                    {
                        SalesOrderId = o.SalesOrderId,
                        SalesOrderCode = o.SalesOrderCode,
                        CustomerName = o.Customer.FullName,
                        SalesOrderStatus = o.SalesOrderStatus,
                        PaymentStatus = o.PaymentStatus,
                        SalesOrderStatusName = o.SalesOrderStatus.ToString(),
                        PaymentStatusName = o.PaymentStatus.ToString(),
                        IsDeposited = o.IsDeposited,
                        PaidFullAt = o.PaidFullAt,
                        TotalPrice = o.TotalPrice,
                        CreateAt = o.CreateAt
                    })
                    .ToListAsync();

                return new ServiceResult<IEnumerable<SalesOrderItemDTO>>
                {
                    StatusCode = 200,
                    Message = "Lấy danh sách đơn hàng thành công.",
                    Data = orders
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ListSalesOrdersAsync");
                return new ServiceResult<IEnumerable<SalesOrderItemDTO>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi lấy danh sách đơn hàng.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<bool>> CheckAndUpdateDeliveredStatusAsync()
        {
            try
            {
                // Lấy tất cả SalesOrder kèm:
                // - SalesOrderDetails (để tính tổng quantity đặt)
                // - StockExportOrders -> GoodsIssueNote -> GoodsIssueNoteDetails (để tính tổng quantity đã xuất)
                var salesOrders = await _unitOfWork.SalesOrder.Query()
                    .Include(so => so.SalesOrderDetails)
                    .Include(so => so.StockExportOrders)
                        .ThenInclude(seo => seo.GoodsIssueNotes)
                            .ThenInclude(gi => gi.GoodsIssueNoteDetails)
                    .ToListAsync();

                if (!salesOrders.Any())
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 200,
                        Message = "Không có đơn hàng nào để kiểm tra Delivered.",
                        Data = true
                    };
                }

                int updatedCount = 0;

                foreach (var order in salesOrders)
                {
                    // Tổng quantity đặt trong SalesOrder
                    var totalOrderedQty = order.SalesOrderDetails?.Sum(d => d.Quantity) ?? 0;

                    // Lấy tất cả GoodsIssueNote liên quan đến SalesOrder này qua StockExportOrder
                    var goodsIssueNotes = order.StockExportOrders
                        .Where(seo => seo.GoodsIssueNotes != null)
                        .SelectMany(seo => seo.GoodsIssueNotes!)
                        .ToList();

                    if (!goodsIssueNotes.Any())
                    {
                        // Chưa có phiếu xuất nào -> không thể Delivered
                        continue;
                    }

                    // Tổng quantity đã xuất trong tất cả GoodsIssueNoteDetail
                    var totalExportedQty = goodsIssueNotes
                        .SelectMany(gi => gi.GoodsIssueNoteDetails)
                        .Sum(d => d.Quantity);


                    // Tất cả GoodsIssueNote phải ở trạng thái Sent
                    bool allSent = goodsIssueNotes.All(gi => gi.Status == GoodsIssueNoteStatus.Sent);

                    if (allSent &&
                        totalExportedQty == totalOrderedQty &&
                        order.SalesOrderStatus != SalesOrderStatus.Delivered)
                    {
                        order.SalesOrderStatus = SalesOrderStatus.Delivered;
                        _unitOfWork.SalesOrder.Update(order);
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    await _unitOfWork.CommitAsync();
                }

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = updatedCount > 0
                        ? $"Cập nhật trạng thái Delivered cho {updatedCount} đơn hàng."
                        : "Không có đơn hàng nào đủ điều kiện chuyển sang Delivered.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi RecalculateDeliveredStatusAsync khi kiểm tra Quantity của GoodsIssueNote và SalesOrder.");

                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi cập nhật trạng thái Delivered cho đơn hàng.",
                    Data = false
                };
            }
        }
    }
}
