using AutoMapper;
using Castle.Core.Resource;
using Microsoft.AspNetCore.Identity;
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

                if (order.CustomerDebts == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng chưa có bản ghi công nợ khách hàng.",
                        Data = false
                    };
                }

                var depositPercent = order.SalesQuotation.DepositPercent;
                var depositAmount = Math.Round(order.TotalPrice * depositPercent / 100m, 2);

                // Update PaidAmount  + DebtAmount
                if (status == PaymentStatus.Deposited)
                {
                    order.PaymentStatus = PaymentStatus.Deposited;
                    order.IsDeposited = true;

                    order.PaidAmount = depositAmount;
                    order.CustomerDebts.DebtAmount = order.TotalPrice - depositAmount;
                }
                else if (status == PaymentStatus.Paid)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.IsDeposited = true;

                    order.PaidAmount = order.TotalPrice;
                    order.CustomerDebts.DebtAmount = 0;
                }
                else
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Trạng thái thanh toán không hợp lệ để xác nhận.",
                        Data = false
                    };
                }

                // Set trạng thái nợ
                var debt = order.CustomerDebts;
                var remainingDebt = debt.DebtAmount;
                var now = DateTime.Now;
                var expired = order.SalesOrderExpiredDate;

                if (remainingDebt <= 0)
                {
                    debt.status = CustomerDebtStatus.NoDebt; // Hết nợ
                }
                else if (order.PaidAmount == 0)
                {
                    debt.status = now > expired
                        ? CustomerDebtStatus.BadDebt   // quá hạn + chưa trả
                        : CustomerDebtStatus.UnPaid;    // chưa đến hạn
                }
                else // PaidAmount > 0 && remainingDebt > 0
                {
                    debt.status = now > expired
                        ? CustomerDebtStatus.OverTime   // quá hạn nhưng đã trả một phần
                        : CustomerDebtStatus.Apart;     // đang nợ một phần, chưa quá hạn
                }


                _unitOfWork.SalesOrder.Update(order);
                _unitOfWork.CustomerDebt.Update(debt);

                await _unitOfWork.CommitAsync();

                await RecalculateTotalReceiveAsync();

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
                        Message = "Có trường dữ liệu trống, vui lòng kiểm tra lại!",
                        Data = null
                    };

                if (string.IsNullOrWhiteSpace(req.CreateBy))
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Người tạo đơn hàng là bắt buộc.",
                        Data = null
                    };

                if (req.Details == null || req.Details.Count == 0)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Danh sách chi tiết trống.",
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
                        Message = "Không tìm thấy báo giá.",
                        Data = null
                    };

                if (sq.ExpiredDate < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Báo giá đã hết hạn. Không thể tạo đơn nháp.",
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
                            Message = "Tồn tại hàng hóa có số lượng không hợp lệ, vui lòng kiểm tra lại!",
                            Data = null
                        };

                    if (!sqDetailByKey.ContainsKey(it.LotId))
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = "Có sản phẩm không thuộc báo giá hiện tại của bạn, vui lòng kiểm tra lại!",
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
                    SalesOrderExpiredDate = DateTime.Now.AddDays(sq.ExpectedDeliveryDate + 3) 
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
                    Message = "Tạo bản nháp đơn hàng từ báo giá thành công.",
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
                        Message = "Không tìm thấy đơn hàng",
                        Data = false
                    };
                }

                if (so.SalesOrderStatus != SalesOrderStatus.Draft)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được xoá đơn ở trạng thái nháp",
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

                try
                {
                    await _noti.SendNotificationToRolesAsync(
                        customerId,
                        new List<string> { "ACCOUNTANT" },
                        "Yêu cầu kiểm tra cọc",
                        $"Khách hàng yêu cầu kiểm tra cọc cho đơn hàng có mã {order.SalesOrderCode}. Vui lòng kiểm tra và xác nhận.",
                        NotificationType.Message
                    );
                }
                catch (Exception notiEx)
                {
                    _logger.LogError(notiEx, "Không gửi được noti yêu cầu kiểm tra cọc (SalesOrderCode={SalesOrderCode})", order.SalesOrderCode);
                }

                return new ServiceResult<bool>
                {
                    StatusCode = 201,
                    Message = "Tạo bản nháp yêu cầu kiểm tra cọc thành công.",
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

        //Gửi yêu cầu cho kế toán 



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

        //List check request for accountant
        public async Task<ServiceResult<IEnumerable<SalesOrderDepositCheckItemDTO>>> ListDepositChecksAsync(DepositCheckStatus? status = null)
        {
            try
            {
                var query = _unitOfWork.SalesOrderDepositCheck.Query()
                    .Include(x => x.SalesOrder)
                        .ThenInclude(o => o.Customer)
                    .OrderByDescending(x => x.RequestedAt)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(x => x.Status == status.Value);
                }

                var items = await query
                    .Select(x => new SalesOrderDepositCheckItemDTO
                    {
                        Id = x.Id,
                        SalesOrderId = x.SalesOrderId,
                        SalesOrderCode = x.SalesOrder.SalesOrderCode,
                        CustomerName = x.SalesOrder.Customer.FullName,
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
                _logger.LogError(ex, "Lỗi ListDepositChecksAsync");
                return new ServiceResult<IEnumerable<SalesOrderDepositCheckItemDTO>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi lấy danh sách yêu cầu kiểm tra cọc.",
                    Data = null
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
                            .ThenInclude(a => a.Supplier)
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
                    ProductId = d.LotProduct.ProductID,
                    ProductName = d.LotProduct.Product.ProductName,
                    d.Quantity,
                    SupplierId = d.LotProduct.Supplier.Id,
                    supplierName = d.LotProduct.Supplier.Name,
                    d.UnitPrice,
                    d.SubTotalPrice,
                    Lot = d.LotProduct == null ? null : new
                    {
                        d.LotProduct.InputDate,
                        d.LotProduct.ExpiredDate,
                        name = d.LotProduct.Supplier.Name
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
                    ExpectedDeliveryDate = order.SalesQuotation.ExpectedDeliveryDate,
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
                    .Include(o => o.SalesQuotation)
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
                        DepositPercent = o.SalesQuotation.DepositPercent,
                        DepositAmount = o.TotalPrice * o.SalesQuotation.DepositPercent,
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
                        Message = "Không tìm thấy đơn hàng",
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
                        Message = "Không tìm thấy đơn hàng",
                        Data = null
                    };
                }

                if (so.SalesOrderStatus != SalesOrderStatus.Draft)
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được gửi đơn ở trạng thái nháp",
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
        public async Task<ServiceResult<object>> UpdateDraftQuantitiesAsync(int orderId, UpdateDraftQuantitiesDTO upd, string? customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                    return new ServiceResult<object>
                    {
                        StatusCode = 401,
                        Message = "Không xác định được người dùng.",
                        Data = null
                    };

                // Validate payload
                if (upd == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Có trường dữ liệu trống, vui lòng kiểm tra lại!",
                        Data = null
                    };

                if (orderId <= 0)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Mã định danh của đơn hàng là bắt buộc.",
                        Data = null
                    };


                if (upd.Details == null || upd.Details.Count == 0)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Danh sách chi tiết trống.",
                        Data = null
                    };

                // Kiểm tra quantity âm sớm để fail fast
                foreach (var it in upd.Details)
                {
                    if (it.Quantity < 0)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Có sản phẩm có số lượng âm, vui lòng kiểm tra lại!",
                            Data = null
                        };
                }

                var duplicatedLot = upd.Details
                    .GroupBy(x => x.LotId)
                    .FirstOrDefault(g => g.Count() > 1);

                if (duplicatedLot != null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = $"LotId {duplicatedLot.Key} bị trùng trong danh sách cập nhật.",
                        Data = null
                    };

                // Lấy SalesOrder + Details + Debt
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

                if (order == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng.",
                        Data = null
                    };

                // Chỉ người tạo đơn mới được phép chỉnh sửa
                if (!string.Equals(order.CreateBy?.Trim(), customerId?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 403,
                        Message = "Bạn không có quyền chỉnh sửa đơn hàng này.",
                        Data = null
                    };
                }

                if (order.SalesOrderStatus != SalesOrderStatus.Draft)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được phép sửa đơn hàng ở trạng thái nháp.",
                        Data = null
                    };

                // SalesOrder expired => cannot update
                if (order.SalesOrderExpiredDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Bản nháp đơn hàng đã hết hạn. Không thể cập nhật số lượng.",
                        Data = null
                    };

                var orderDetailByKey = order.SalesOrderDetails
                    .ToDictionary(d => d.LotId);


                foreach (var it in upd.Details)
                {
                    if (!orderDetailByKey.ContainsKey(it.LotId))
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"LotId {it.LotId} không thuộc đơn hàng, vui lòng kiểm tra lại.",
                            Data = null
                        };
                }

                await _unitOfWork.BeginTransactionAsync();

                foreach (var it in upd.Details)
                {
                    var d = orderDetailByKey[it.LotId];

                    d.Quantity = it.Quantity;
                    d.SubTotalPrice = it.Quantity > 0
                        ? Math.Round(d.UnitPrice * it.Quantity, 2)
                        : 0m;

                    _unitOfWork.SalesOrderDetails.Update(d);
                }

                order.TotalPrice = order.SalesOrderDetails.Sum(x => x.SubTotalPrice);
                
                _unitOfWork.SalesOrder.Update(order);

                var debt = await _unitOfWork.CustomerDebt.Query()
                    .FirstOrDefaultAsync(x => x.SalesOrderId == order.SalesOrderId);

                if (debt != null)
                {
                    debt.DebtAmount = Math.Max(0m, order.TotalPrice - order.PaidAmount);
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
                    Message = "Cập nhật số lượng cho bản nháp đơn hàng thành công.",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi UpdateDraftQuantitiesAsync({SalesOrderId})", orderId);

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
                        DepositPercent = o.SalesQuotation.DepositPercent,
                        DepositAmount = o.TotalPrice * o.SalesQuotation.DepositPercent,
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
                        Message = "Không tìm thấy đơn hàng",
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

                if (so.SalesOrderExpiredDate != default &&
                    so.SalesOrderExpiredDate.Date < DateTime.Now.Date)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng đã hết hạn, không thể chấp thuận.",
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
                        Message = "Không tìm thấy đơn hàng",
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
                    Message = "Cập nhật tổng thu từ số tiền đã trả của đơn bán hàng thành công.",
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
                    .Where(s => s.SalesOrderStatus == SalesOrderStatus.Approved && (s.SalesQuotation.DepositPercent == 0 
                    || (s.SalesQuotation.DepositPercent > 0 && s.IsDeposited == true)) 
                    || s.SalesOrderStatus == SalesOrderStatus.PartiallyDelivered && (s.SalesQuotation.DepositPercent == 0
                    || (s.SalesQuotation.DepositPercent > 0 && s.IsDeposited == true)))
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

                    if (totalOrderedQty <= 0) continue;

                    // Lấy tất cả GoodsIssueNote liên quan đến SalesOrder này qua StockExportOrder
                    var goodsIssueNotes = order.StockExportOrders
                        .Where(seo => seo.GoodsIssueNotes != null)
                        .SelectMany(seo => seo.GoodsIssueNotes!)
                        .Where(gi => gi.Status == GoodsIssueNoteStatus.Exported)
                        .ToList();

                    //Chưa có phiếu xuất nào thì không đổi trạng thái
                    if (!goodsIssueNotes.Any())
                    {
                        continue;
                    }

                    // Tổng quantity đã xuất trong tất cả GoodsIssueNoteDetail
                    var totalExportedQty = goodsIssueNotes
                        .SelectMany(gi => gi.GoodsIssueNoteDetails)
                        .Sum(d => d.Quantity);

                    if (totalExportedQty <= 0)
                    {
                        continue;
                    }

                    SalesOrderStatus? newStatus = null;

                    if (totalExportedQty >= totalOrderedQty)
                    {
                        newStatus = SalesOrderStatus.Delivered;
                    }
                    else 
                    {
                        newStatus = SalesOrderStatus.PartiallyDelivered;
                    }

                    if (newStatus.HasValue && order.SalesOrderStatus != newStatus.Value)
                    {
                        order.SalesOrderStatus = newStatus.Value;
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

        public async Task<ServiceResult<bool>> MarkBackSalesOrderAsync(int salesOrderId, string staffId)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.StockExportOrders)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (order == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy đơn hàng.", 404);

                var hasNotEnough = order.StockExportOrders != null
                    && order.StockExportOrders.Any(seo => seo.Status == StockExportOrderStatus.NotEnough);

                if (!hasNotEnough)
                    return ServiceResult<bool>.Fail(
                        "Không thể chuyển sang trạng thái chờ hàng vì không có phiếu xuất kho ở trạng thái không đủ hàng.", 400);

                if (order.SalesOrderStatus != SalesOrderStatus.Approved &&
                    order.SalesOrderStatus != SalesOrderStatus.PartiallyDelivered)
                {
                    return ServiceResult<bool>.Fail(
                        "Chỉ được chuyển sang trạng thái chờ hàng khi đơn đang ở trạng thái được chấp thuận hoặc đã giao hàng một phần.", 400);
                }

                order.SalesOrderStatus = SalesOrderStatus.BackSalesOrder;
                _unitOfWork.SalesOrder.Update(order);

                await _unitOfWork.CommitAsync();

                try
                {
                    await _noti.SendNotificationToCustomerAsync(
                        senderId: staffId,
                        receiverId: order.CreateBy,
                        title: "Đơn hàng tạm chuyển sang chờ hàng",
                        message: $"Đơn hàng {order.SalesOrderCode} hiện đang thiếu hàng để xuất kho. " +
                                 $"Hệ thống đã chuyển trạng thái sang Chờ hàng (BackSalesOrder). " +
                                 $"Nhà thuốc sẽ liên hệ khi hàng sẵn sàng.",
                        type: NotificationType.Warning
                    );
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification thất bại khi chuyển BackSalesOrder: orderId={OrderId}, receiver={ReceiverId}",
                        order.SalesOrderId, order.CreateBy);
                }

                return ServiceResult<bool>.SuccessResult(true,
                    "Đã chuyển đơn hàng sang trạng thái chờ hàng.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkBackSalesOrderAsync({SalesOrderId}) error", salesOrderId);
                return ServiceResult<bool>.Fail("Có lỗi khi cập nhật trạng thái BackSalesOrder.", 500);
            }
        }

        public async Task<ServiceResult<bool>> MarkNotCompleteAndRefundAsync(int salesOrderId, string staffId, string rejectReason)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.CustomerDebts)
                    .Include(o => o.SalesQuotation)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (order == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.SalesOrderStatus == SalesOrderStatus.Complete)
                    return ServiceResult<bool>.Fail("Đơn hàng đã hoàn thành, không thể chuyển trạng thái không hoàn thành.", 400);

                if (order.SalesQuotation == null)
                    return ServiceResult<bool>.Fail("Đơn hàng không có thông tin báo giá để tính tiền cọc.", 400);

                await _unitOfWork.BeginTransactionAsync();

                //Tính số tiền đã trả
                var depositPercent = order.SalesQuotation.DepositPercent;
                var depositAmount = Math.Round(order.TotalPrice * depositPercent / 100m, 2);

                //Tính tổng giá trị phiếu xuất đã xuất
                var exportedValue = await (
                    from gin in _unitOfWork.GoodsIssueNote.Query().AsNoTracking()
                    where gin.StockExportOrder.SalesOrderId == order.SalesOrderId
                          && gin.Status == GoodsIssueNoteStatus.Exported
                    from gd in gin.GoodsIssueNoteDetails
                    join sod in _unitOfWork.SalesOrderDetails.Query().AsNoTracking()
                            .Where(x => x.SalesOrderId == order.SalesOrderId)
                        on gd.LotId equals sod.LotId
                    select (decimal?)gd.Quantity * sod.UnitPrice
                ).SumAsync() ?? 0m;

                exportedValue = Math.Round(exportedValue, 2);

                //Cập nhật số tiền đã trả theo công thức PaidAmount = PaidAmount - (DepositAmount - ((ExportedValue/TotalPrice) * DepositAmount))
                decimal ratio = 0m;
                if (order.TotalPrice > 0m)
                {
                    ratio = exportedValue / order.TotalPrice;
                    if (ratio < 0m) ratio = 0m;
                    if (ratio > 1m) ratio = 1m;
                }

                var subtractAmount = Math.Round(depositAmount - (ratio * depositAmount), 2);
                if (subtractAmount < 0m) subtractAmount = 0m;

                order.PaidAmount = Math.Max(0m, Math.Round(order.PaidAmount - subtractAmount, 2));

                //Cập nhật trạng thái đơn hàng và trạng thái thanh toán
                order.SalesOrderStatus = SalesOrderStatus.NotComplete;
                order.PaymentStatus = PaymentStatus.Refunded;

                if (string.IsNullOrWhiteSpace(rejectReason))
                    return ServiceResult<bool>.Fail("Vui lòng nhập lý do từ chối.", 400);

                order.RejectReason = rejectReason.Trim();

                order.RejectedAt = DateTime.UtcNow; 
                order.RejectedBy = staffId;

                //Cập nhật nợ của khách cho đơn hàng này
                if (order.CustomerDebts != null)
                {
                    order.CustomerDebts.DebtAmount = 0m;
                    order.CustomerDebts.status = CustomerDebtStatus.Disable;
                    _unitOfWork.CustomerDebt.Update(order.CustomerDebts);
                }

                _unitOfWork.SalesOrder.Update(order);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                try
                {
                    await _noti.SendNotificationToCustomerAsync(
                        senderId: staffId,
                        receiverId: order.CreateBy,
                        title: "Đơn hàng không hoàn tất và đã cập nhật hoàn tiền",
                        message: $"Đơn hàng {order.SalesOrderCode} đã được chuyển sang trạng thái Không hoàn thành (NotComplete). " +
                                 $"Trạng thái thanh toán đã được cập nhật sang Hoàn tiền (Refunded). " +
                                 $"Vui lòng kiểm tra thông tin nhận tiền/trao đổi với nhà thuốc nếu cần hỗ trợ.",
                        type: NotificationType.Message
                    );
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi thông báo thất bại khi chuyển NotComplete/Refunded: orderId={OrderId}, receiver={ReceiverId}",
                        order.SalesOrderId, order.CreateBy);
                }

                return ServiceResult<bool>.SuccessResult(true,
                    "Đã chuyển đơn hàng sang trạng thái không hoàn thành và cập nhật trạng thái thanh toán là đã hoàn tiền.", 200);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "MarkNotCompleteAndRefundAsync({SalesOrderId}) error", salesOrderId);
                return ServiceResult<bool>.Fail("Có lỗi khi chuyển NotComplete và cập nhật Refunded.", 500);
            }
        }

        public async Task<int> AutoMarkNotCompleteWhenDepositOverdueAsync()
        {
            var now = DateTime.Now; 

            // Chỉ check các đơn đang Approved + chưa cọc đồng nào + có quy định cọc
            var orders = await _unitOfWork.SalesOrder.Query()
                .Include(o => o.SalesQuotation)
                .Include(o => o.CustomerDebts)
                .Where(o => o.SalesOrderStatus == SalesOrderStatus.Approved
                            && o.PaidAmount == 0
                            && o.SalesQuotation != null
                            && o.SalesQuotation.DepositPercent > 0
                            && o.SalesQuotation.DepositDueDays > 0)
                .ToListAsync();

            int updated = 0;

            foreach (var o in orders)
            {
                var depositDueAt = o.CreateAt.AddDays(o.SalesQuotation.DepositDueDays);

                if (now > depositDueAt)
                {
                    o.SalesOrderStatus = SalesOrderStatus.NotComplete;
                    o.PaymentStatus = PaymentStatus.NotPaymentYet;
                    o.IsDeposited = false;

                    if (o.CustomerDebts != null)
                    {
                        o.CustomerDebts.status = CustomerDebtStatus.Disable;
                        o.CustomerDebts.DebtAmount = 0m;
                        _unitOfWork.CustomerDebt.Update(o.CustomerDebts);
                    }

                    _unitOfWork.SalesOrder.Update(o);
                    updated++;
                }
            }

            if (updated > 0)
                await _unitOfWork.CommitAsync();

            return updated;
        }


        #region statistics 
        public async Task<ServiceResult<List<MonthlyRevenueDTO>>> GetYearRevenueAsync(int year)
        {
            try
            {
                // Lọc các đơn đã thanh toán đủ trong năm đó
                var query = _unitOfWork.SalesOrder.Query()
                    .AsNoTracking()
                    .Where(o =>
                        o.CreateAt.Year == year &&
                        (o.PaymentStatus == PaymentStatus.Deposited ||
                         o.PaymentStatus == PaymentStatus.PartiallyPaid ||
                         o.PaymentStatus == PaymentStatus.Paid ||
                         o.PaymentStatus == PaymentStatus.Refunded));

                // Group theo tháng, sum PaidAmount
                var monthlyData = await query
                    .GroupBy(o => o.CreateAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Amount = g.Sum(x => x.PaidAmount)
                    })
                    .ToListAsync();

                var totalYearRevenue = monthlyData.Sum(x => x.Amount);

                var result = Enumerable.Range(1, 12)
                    .Select(m =>
                    {
                        var data = monthlyData.FirstOrDefault(x => x.Month == m);
                        var amount = data?.Amount ?? 0m;

                        var percentage = totalYearRevenue == 0
                            ? 0m
                            : Math.Round((amount / totalYearRevenue) * 100, 2);

                        return new MonthlyRevenueDTO
                        {
                            Month = m,
                            Amount = amount,
                            Percentage = percentage
                        };
                    })
                    .ToList();

                return new ServiceResult<List<MonthlyRevenueDTO>>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<MonthlyRevenueDTO>>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống: " + ex.Message,
                    Data = null
                };
            }
        }

        /// <summary>
        /// Thống kê số lượng sản phẩm bán theo năm:
        /// - Input: year
        /// - Output: chia theo tháng, trong mỗi tháng có % từng mặt hàng (LotId).
        /// </summary>
        public async Task<ServiceResult<List<MonthlyProductStatisticDTO>>> GetProductQuantityByYearAsync(int year)
        {
            try
            {
                var ginQuery = _unitOfWork.GoodsIssueNote.Query().AsNoTracking();
                var gindQuery = _unitOfWork.GoodsIssueNoteDetails.Query().AsNoTracking();
                var lotQuery = _unitOfWork.LotProduct.Query().AsNoTracking();
                var prodQuery = _unitOfWork.Product.Query().AsNoTracking();

                var raw = await (
                    from gin in ginQuery
                    join gind in gindQuery on gin.Id equals gind.GoodsIssueNoteId
                    join lot in lotQuery on gind.LotId equals lot.LotID
                    join p in prodQuery on lot.ProductID equals p.ProductID
                    where gin.Status == GoodsIssueNoteStatus.Exported
                          && (
                                (gin.ExportedAt.HasValue && gin.ExportedAt.Value.Year == year)
                                || (!gin.ExportedAt.HasValue && gin.CreateAt.Year == year)
                             )
                    select new
                    {
                        Month = gin.ExportedAt.HasValue ? gin.ExportedAt.Value.Month : gin.CreateAt.Month,
                        LotId = gind.LotId,
                        Quantity = gind.Quantity,

                        ProductId = p.ProductID,
                        ProductName = p.ProductName,
                        UnitName = p.Unit,
                        ExpiredDate = lot.ExpiredDate
                    }
                ).ToListAsync();

                var grouped = raw
                    .GroupBy(x => new
                    {
                        x.Month,
                        x.LotId,
                        x.ProductId,
                        x.ProductName,
                        x.UnitName,
                        x.ExpiredDate
                    })
                    .Select(g => new
                    {
                        g.Key.Month,
                        g.Key.LotId,
                        g.Key.ProductId,
                        g.Key.ProductName,
                        g.Key.UnitName,
                        g.Key.ExpiredDate,
                        Quantity = g.Sum(x => x.Quantity)
                    })
                    .ToList();

                var result = new List<MonthlyProductStatisticDTO>();

                for (int month = 1; month <= 12; month++)
                {
                    var monthItems = grouped
                        .Where(x => x.Month == month)
                        .ToList();

                    var totalQuantityOfMonth = monthItems.Sum(x => x.Quantity);

                    var productList = monthItems
                        .Select(x =>
                        {
                            decimal percentage = totalQuantityOfMonth == 0
                                ? 0m
                                : Math.Round((decimal)x.Quantity / totalQuantityOfMonth * 100, 2);

                            return new ProductPercentageDTO
                            {
                                LotId = x.LotId,
                                Quantity = x.Quantity,
                                Percentage = percentage,
                                Product = new ProductInfoDTO
                                {
                                    ProductId = x.ProductId,
                                    ProductName = x.ProductName ?? string.Empty,
                                    UnitName = x.UnitName ?? string.Empty,
                                    ExpiredDate = x.ExpiredDate
                                }
                            };
                        })
                        .OrderByDescending(p => p.Quantity)
                        .ToList();

                    result.Add(new MonthlyProductStatisticDTO
                    {
                        Month = month,
                        TotalQuantity = totalQuantityOfMonth,
                        Products = productList
                    });
                }

                return new ServiceResult<List<MonthlyProductStatisticDTO>>
                {
                    StatusCode = 200,
                    Message = "Thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<MonthlyProductStatisticDTO>>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống: " + ex.Message,
                    Data = null
                };
            }
        }

        #endregion
    }
}
