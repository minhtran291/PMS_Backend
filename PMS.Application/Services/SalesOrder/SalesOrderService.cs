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

                if (order.SalesOrderStatus != SalesOrderStatus.Approved && order.PaymentStatus != PaymentStatus.Deposited)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ xác nhận thanh toán cho đơn ở trạng thái đã được chấp thuận hoặc đã cọc.",
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


        public async Task<ServiceResult<object>> GetOrderDetailsAsync(int salesOrderId)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        //.ThenInclude(d => d.Product)
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(d => d.LotProduct)

                    .Include(o => o.SalesQuotation)
                        .ThenInclude(q => q.SalesQuotaionDetails)
                            .ThenInclude(qd => qd.Product)
                    .Include(o => o.SalesQuotation)
                        .ThenInclude(q => q.SalesQuotaionDetails)
                            .ThenInclude(qd => qd.LotProduct)
                    .Include(o => o.SalesQuotation)
                        .ThenInclude(q => q.SalesQuotaionDetails)
                            .ThenInclude(qd => qd.TaxPolicy)

                    .Include(o => o.CustomerDebts)

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
                    //d.ProductId,
                    d.LotId,
                    //ProductName = d.Product?.ProductName,
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
                    order.TotalPrice,
                    order.PaidAmount,
                    order.SalesOrderExpiredDate,

                    CustomerDebt = order.CustomerDebts,

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
                    .AsNoTracking()
                    .Where(o => o.CreateBy == userId)
                    .OrderByDescending(o => o.CreateAt)
                    .Select(o => new SalesOrderItemDTO
                    {
                        SalesOrderId = o.SalesOrderId,
                        SalesOrderCode = o.SalesOrderCode,
                        Status = o.SalesOrderStatus,
                        StatusName = o.SalesOrderStatus.ToString(),
                        IsDeposited = o.IsDeposited,
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
                        "261b6651-7d07-4267-bc71-d70b32bae334",
                        new List<string> { "PURCHASES_STAFF" },
                        "Thiếu hàng khi khách gửi SalesOrder",
                        $"Các mặt hàng thiếu/sắp hết: {msg}",
                        NotificationType.Warning
                    );
                    await _noti.SendNotificationToRolesAsync(
                        "261b6651-7d07-4267-bc71-d70b32bae334",
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
                    var debt = new CustomerDebt
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
                    .AsNoTracking()
                    .OrderByDescending(o => o.CreateAt)
                    .Select(o => new SalesOrderItemDTO
                    {
                        SalesOrderId = o.SalesOrderId,
                        SalesOrderCode = o.SalesOrderCode,
                        Status = o.SalesOrderStatus,
                        StatusName = o.SalesOrderStatus.ToString(),
                        IsDeposited = o.IsDeposited,
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

        public async Task<ServiceResult<bool>> ApproveSalesOrderAsync(int salesOrderId)
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
                    var title = "Đơn hàng đã được chấp thuận";
                    var message = $"Đơn {so.SalesOrderCode} của bạn đã được chấp thuận.";

                    // senderId: có thể là system hoặc user duyệt đơn (tùy hệ thống bạn)
                    var senderId = "system";
                    var receiverId = so.CreateBy;

                    await _noti.SendNotificationToCustomerAsync(
                        senderId: senderId,
                        receiverId: receiverId,
                        title: title,
                        message: message,
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

        public async Task<ServiceResult<bool>> RejectSalesOrderAsync(int salesOrderId)
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

                so.SalesOrderStatus = SalesOrderStatus.Rejected;
                _unitOfWork.SalesOrder.Update(so);
                await _unitOfWork.CommitAsync();

                try
                {
                    var title = "Đơn hàng đã bị từ chối";
                    var message = $"Đơn {so.SalesOrderCode} của bạn đã bị từ chối vì lý do thiếu hàng.";

                    // senderId: có thể là system hoặc user duyệt đơn (tùy hệ thống bạn)
                    var senderId = "system";
                    var receiverId = so.CreateBy;

                    await _noti.SendNotificationToCustomerAsync(
                        senderId: senderId,
                        receiverId: receiverId,
                        title: title,
                        message: message,
                        type: NotificationType.Message
                    );
                }
                catch (Exception exNotify)
                {
                    // Không rollback trạng thái; chỉ log cảnh báo khi gửi noti lỗi
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
                        Status = o.SalesOrderStatus,
                        StatusName = o.SalesOrderStatus.ToString(),
                        IsDeposited = o.IsDeposited,
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
