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


        public async Task<ServiceResult<bool>> ConfirmPaymentAsync(int salesOrderId, SalesOrderStatus status)
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

                if (order.Status != SalesOrderStatus.Approved && order.Status != SalesOrderStatus.Deposited)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ xác nhận thanh toán cho đơn ở trạng thái đã được chấp thuận hoặc đã cọc.",
                        Data = false
                    };
                }

                decimal depositAmount = order.TotalPrice * decimal.Round(order.SalesQuotation.DepositPercent / 100, 2);

                if (status == SalesOrderStatus.Deposited)
                {
                    order.Status = status;
                    order.IsDeposited = true;
                    order.PaidAmount = depositAmount;
                    order.CustomerDebts.DebtAmount = order.TotalPrice - depositAmount;
                    if (DateTime.Now > order.SalesOrderExpiredDate)
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.BadDebt;
                    }
                    else
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.OnTime;
                    }
                }

                if (status == SalesOrderStatus.Paid)
                {
                    order.Status = status;
                    order.IsDeposited = true;
                    order.PaidAmount = order.TotalPrice;
                    order.CustomerDebts.DebtAmount = 0;
                    if (DateTime.Now > order.SalesOrderExpiredDate)
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.BadDebt;
                    }
                    else
                    {
                        order.CustomerDebts.status = CustomerDebtStatus.OnTime;
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
                    .Include(q => q.SalesQuotaionDetails).ThenInclude(d => d.Product)
                    .Include(q => q.SalesQuotaionDetails).ThenInclude(d => d.LotProduct)
                    .Include(q => q.SalesQuotaionDetails).ThenInclude(d => d.TaxPolicy)
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
                    .ToDictionary(d => (d.ProductId, d.LotId), d => d);

                foreach (var it in req.Details)
                {
                    if (it.Quantity < 0)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Quantity âm ở ProductId={it.ProductId}.",
                            Data = null
                        };

                    var key = (it.ProductId, it.LotId);
                    if (!sqDetailByKey.ContainsKey(key))
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = $"Dòng không thuộc báo giá: ProductId={it.ProductId}, LotId={it.LotId}.",
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
                    Status = SalesOrderStatus.Draft,
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
                    var sqd = sqDetailByKey[(it.ProductId, it.LotId)];

                    var basePrice = sqd.LotProduct?.SalePrice ?? 0m;

                    var taxRate = sqd.TaxPolicy?.Rate ?? 0m; 

                    var serverUnitPrice = decimal.Round(basePrice * (1 + taxRate), 2);

                    var sub = (it.Quantity > 0) ? decimal.Round(serverUnitPrice * it.Quantity, 2) : 0m;

                    detailEntities.Add(new SalesOrderDetails
                    {
                        SalesOrderId = order.SalesOrderId,
                        //ProductId = it.ProductId,
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

                //Customer dept
                var debt = new CustomerDebt
                {
                    CustomerId = req.CreateBy,
                    SalesOrderId = order.SalesOrderId,
                    DebtAmount = order.TotalPrice - order.PaidAmount,
                    status = CustomerDebtStatus.Maturity
                };
                await _unitOfWork.CustomerDebt.AddAsync(debt);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var data = new
                {
                    order.SalesOrderId,
                    order.SalesOrderCode,
                    order.SalesQuotationId,
                    order.CreateBy,
                    order.CreateAt,
                    order.Status,
                    order.IsDeposited,
                    order.TotalPrice,
                    order.SalesOrderExpiredDate,
                    CustomerDebt = new
                    {
                        debt.CustomerId,
                        debt.SalesOrderId,
                        debt.DebtAmount,
                        debt.status
                    },
                    Details = detailEntities.Select(d => new
                    {
                        //d.ProductId,
                        d.LotId,
                        //ProductName = sqDetailByKey[(d.ProductId, d.LotId)].Product.ProductName,
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

                if (so.Status != SalesOrderStatus.Draft)
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
                    order.Status,
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
                        Status = o.Status,
                        StatusName = o.Status.ToString(),
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

                if (so.Status != SalesOrderStatus.Paid && so.Status != SalesOrderStatus.Deposited)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được hoàn tất đơn khi đã thanh toán (Paid/Deposited)",
                        Data = false
                    };
                }

                so.Status = SalesOrderStatus.Complete;
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
                        //.ThenInclude(d => d.Product)
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

                if (so.Status != SalesOrderStatus.Draft)
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
                    //if (d.ProductId == null) continue;

                    var totalAvailable = await _unitOfWork.LotProduct.Query()
                        //.Where(l => l.ProductID == d.ProductId && l.ExpiredDate > DateTime.Now && l.LotQuantity > 0)
                        .SumAsync(l => (int?)l.LotQuantity) ?? 0;

                    if (d.Quantity > totalAvailable)
                    {
                        //var prodName = d.Product?.ProductName ?? $"Product {d.ProductId}";
                        //var missing = d.Quantity - totalAvailable;
                        //warnings.Add($"{prodName}: thiếu {missing}");
                    }
                }

                if (warnings.Count > 0)
                {
                    var msg = string.Join("; ", warnings);
                    await _noti.SendNotificationToRolesAsync(
                        "system",
                        new List<string> { "PURCHASES_STAFF" },
                        "Thiếu hàng khi khách gửi SalesOrder",
                        $"Các mặt hàng thiếu/sắp hết: {msg}",
                        NotificationType.Warning
                    );
                }

                so.Status = SalesOrderStatus.Send;
                _unitOfWork.SalesOrder.Update(so);
                await _unitOfWork.CommitAsync();

                var data = new
                {
                    so.SalesOrderId,
                    so.Status,
                    so.TotalPrice,
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

                if (order.Status != SalesOrderStatus.Draft)
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
                    order.Status,
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
                        Status = o.Status,
                        StatusName = o.Status.ToString(),
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

                if (so.Status != SalesOrderStatus.Send)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi!",
                        Data = false
                    };
                }

                so.Status = SalesOrderStatus.Approved;
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

                if (so.Status != SalesOrderStatus.Send)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi!",
                        Data = false
                    };
                }

                so.Status = SalesOrderStatus.Rejected;
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
    }
}
