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

                order.Status = status;
                order.IsDeposited = true;
                order.PaidAmount = order.TotalPrice;

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

        //public async Task<ServiceResult<object>> CreateDraftFromSalesQuotationAsync(SalesOrderRequestDTO req)
        //{
        //    try
        //    {
        //        if (req == null)
        //            return new ServiceResult<object> { 
        //                StatusCode = 400, 
        //                Message = "Payload trống.", 
        //                Data = null 
        //            };

        //        if (string.IsNullOrWhiteSpace(req.CreateBy))
        //            return new ServiceResult<object> { 
        //                StatusCode = 400, 
        //                Message = "CreateBy là bắt buộc.", 
        //                Data = null 
        //            };

        //        if (req.Details == null || req.Details.Count == 0)
        //            return new ServiceResult<object> { 
        //                StatusCode = 400, 
        //                Message = "Danh sách Details trống.", 
        //                Data = null 
                    
        //            };

        //        var sq = await _unitOfWork.SalesQuotation.Query()
        //            .Include(q => q.SalesQuotaionDetails).ThenInclude(d => d.Product)
        //            .Include(q => q.SalesQuotaionDetails).ThenInclude(d => d.LotProduct)
        //            .FirstOrDefaultAsync(q => q.Id == req.SalesQuotationId);

        //        if (sq == null)
        //            return new ServiceResult<object> { 
        //                StatusCode = 404, 
        //                Message = "Không tìm thấy SalesQuotation.", 
        //                Data = null 
        //            };

        //        if (sq.ExpiredDate < DateTime.Now.Date)
        //            return new ServiceResult<object> { 
        //                StatusCode = 400, 
        //                Message = "SalesQuotation đã hết hạn. Không thể tạo đơn nháp.", 
        //                Data = null 
        //            };

        //        var sqDetailByKey = sq.SalesQuotaionDetails
        //            .ToDictionary(d => (d.ProductId, d.LotId), d => d);

        //        foreach (var it in req.Details)
        //        {
        //            if (it.Quantity < 0)
        //                return new ServiceResult<object> { 
        //                    StatusCode = 400, 
        //                    Message = $"Quantity âm ở ProductId={it.ProductId}.", 
        //                    Data = null 
        //                };

        //            var key = (it.ProductId, it.LotId);
        //            if (!sqDetailByKey.ContainsKey(key))
        //                return new ServiceResult<object> { 
        //                    StatusCode = 400, 
        //                    Message = $"Dòng không thuộc báo giá: ProductId={it.ProductId}, LotId={it.LotId}.", 
        //                    Data = null 
        //                };
        //        }

        //        await _unitOfWork.BeginTransactionAsync();

        //        var order = new Core.Domain.Entities.SalesOrder
        //        {
        //            SalesQuotationId = sq.Id,
        //            SalesOrderCode = GenerateSalesOrderCode(),
        //            CreateBy = req.CreateBy.Trim(),
        //            CreateAt = DateTime.Now,  
        //            Status = SalesOrderStatus.Draft, 
        //            IsDeposited = false,
        //            TotalPrice = 0m,
        //            PaidAmount = 0m
        //        };

        //        await _unitOfWork.SalesOrder.AddAsync(order);
        //        await _unitOfWork.CommitAsync();

        //        var detailEntities = new List<SalesOrderDetails>();
        //        foreach (var it in req.Details)
        //        {
        //            var sqd = sqDetailByKey[(it.ProductId, it.LotId)];

        //            //sqd.LotProduct != null ? sqd.LotProduct.SalePrice : it.UnitPrice
        //            var serverUnitPrice = it.UnitPrice + (it.UnitPrice * sqd.TaxPolicy.Rate);

        //            var sub = (it.Quantity > 0) ? serverUnitPrice * it.Quantity : 0m;

        //            detailEntities.Add(new SalesOrderDetails
        //            {
        //                SalesOrderId = order.SalesOrderId,
        //                ProductId = it.ProductId,
        //                LotId = it.LotId,
        //                Quantity = it.Quantity,
        //                UnitPrice = serverUnitPrice,
        //                SubTotalPrice = sub
        //            });
        //        }

        //        if (detailEntities.Count > 0)
        //            await _unitOfWork.SalesOrderDetails.AddRangeAsync(detailEntities);

        //        order.TotalPrice = detailEntities.Sum(d => d.SubTotalPrice);

        //        _unitOfWork.SalesOrder.Update(order);

        //        var debt = new CustomerDebt
        //        {
        //            //CustomerId = CustomerId,
        //            SalesOrderId = order.SalesOrderId,
        //            DebtAmount = order.TotalPrice - order.PaidAmount // = 100% TotalPrice - PaidAmount=0
        //        };
        //        await _unitOfWork.CustomerDebt.AddAsync(debt);

        //        await _unitOfWork.CommitAsync();
        //        await _unitOfWork.CommitTransactionAsync();

        //        var data = new
        //        {
        //            order.SalesOrderId,
        //            order.SalesOrderCode,
        //            order.SalesQuotationId,
        //            order.CreateBy,
        //            order.CreateAt,
        //            order.Status,
        //            order.IsDeposited,
        //            order.TotalPrice,
        //            CustomerDebt = new
        //            {
        //                debt.Id,
        //                debt.CustomerId,
        //                debt.SalesOrderId,
        //                debt.DebtAmount
        //            },
        //            Details = detailEntities.Select(d => new
        //            {
        //                d.ProductId,
        //                d.LotId,
        //                ProductName = sqDetailByKey[(d.ProductId, d.LotId)].Product.ProductName,
        //                d.Quantity,
        //                d.UnitPrice,
        //                d.SubTotalPrice
        //            }).ToList()
        //        };

        //        return new ServiceResult<object>
        //        {
        //            StatusCode = 201,
        //            Message = "Tạo SalesOrder Draft từ SalesQuotation thành công.",
        //            Data = data
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackTransactionAsync();
        //        _logger.LogError(ex, "Lỗi CreateDraftFromSalesQuotationAsync({SalesQuotationId})", req?.SalesQuotationId);
        //        return new ServiceResult<object>
        //        {
        //            StatusCode = 500,
        //            Message = "Có lỗi xảy ra khi tạo bản nháp đơn hàng.",
        //            Data = null
        //        };
        //    }
        //}

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

        public async Task<ServiceResult<VnPayInitResponseDTO>> GenerateVnPayPaymentAsync(int salesOrderId, string paymentType)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query().FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);
                if (order == null)
                {
                    return new ServiceResult<VnPayInitResponseDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng",
                        Data = null
                    };
                }

                if (order.Status != SalesOrderStatus.Approved)
                    return new ServiceResult<VnPayInitResponseDTO> { 
                        StatusCode = 400, 
                        Message = "Chỉ khởi tạo thanh toán cho đơn ở trạng thái đã được chấp thuận." 
                    };


                decimal amount = paymentType.ToLower() == "deposit"
                    ? (order.TotalPrice * order.SalesQuotation.DepositPercent)  
                    : order.TotalPrice;

                var info = paymentType == "deposit" ? "Thanh toán tiền cọc" : "Thanh toán toàn bộ đơn hàng";

                var paymentUrl = _vnPay.CreatePaymentUrl(salesOrderId.ToString(), (long)amount, info);
                var qrData = _vnPay.GenerateQrDataUrl(paymentUrl);

                return new ServiceResult<VnPayInitResponseDTO>
                {
                    StatusCode = 200,
                    Message = "Khởi tạo thanh toán VNPay thành công",
                    Data = new VnPayInitResponseDTO
                    {
                        PaymentUrl = paymentUrl,
                        QrDataUrl = qrData,
                        Amount = amount,
                        PaymentType = paymentType
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi khởi tạo thanh toán VNPay");
                return new ServiceResult<VnPayInitResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Lỗi trong quá trình tạo thanh toán VNPay",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<object>> GetOrderDetailsAsync(int salesOrderId)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(l => l.Product)
                    .Include(q => q.SalesQuotation)
                        .ThenInclude(d => d.SalesQuotaionDetails)
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

                var data = new
                {
                    order. SalesOrderId,
                    order.SalesQuotationId,
                    order.CreateBy,
                    order.CreateAt,
                    order.Status,
                    order.TotalPrice,
                    //order.DepositAmount,
                    Details = order.SalesOrderDetails.Select(d => new
                    {
                        
                        d.Quantity,
                        d.UnitPrice,
                        ProductName = d.Product.ProductName,
                        ExpiredDate = d.SalesOrder.SalesQuotation.ExpiredDate
                    })
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

        ////Customer mark order is complete
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

        ////Customer send the sales order draft
        public async Task<ServiceResult<object>> SendOrderAsync(int salesOrderId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(d => d.Product)
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
                    if (d.ProductId == null) continue;

                    var totalAvailable = await _unitOfWork.LotProduct.Query()
                        .Where(l => l.ProductID == d.ProductId && l.ExpiredDate > DateTime.Now && l.LotQuantity > 0)
                        .SumAsync(l => (int?)l.LotQuantity) ?? 0;

                    if (d.Quantity > totalAvailable)
                    {
                        var prodName = d.Product?.ProductName ?? $"Product {d.ProductId}";
                        var missing = d.Quantity - totalAvailable;
                        warnings.Add($"{prodName}: thiếu {missing}");
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

        // customer update sales order when status is draft
        public async Task<ServiceResult<bool>> UpdateDraftQuantitiesAsync(int salesOrderId, List<SalesOrderDetailsUpdateDTO> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                    return new ServiceResult<bool> { 
                        StatusCode = 400, 
                        Message = "Danh sách cập nhật rỗng.", 
                        Data = false 
                    };

                var so = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (so == null)
                    return new ServiceResult<bool> { 
                        StatusCode = 404, 
                        Message = "Không tìm thấy SalesOrder.", 
                        Data = false 
                    };

                if (so.Status != SalesOrderStatus.Draft)
                    return new ServiceResult<bool> { 
                        StatusCode = 400, 
                        Message = "Chỉ được cập nhật khi đơn ở trạng thái Draft.", 
                        Data = false 
                    };

                var qtyMap = items.ToDictionary(x => x.ProductId, x => x.Quantity);

                await _unitOfWork.BeginTransactionAsync();

                foreach (var d in so.SalesOrderDetails)
                {
                    if (!qtyMap.TryGetValue(d.ProductId, out var newQty)) continue;
                    if (newQty < 0) newQty = 0;

                    d.Quantity = newQty;

                    decimal picked = 0m, costSum = 0m;

                    if (newQty > 0)
                    {
                        var lots = await _unitOfWork.LotProduct.Query()
                            .Where(l => l.ProductID == d.ProductId && l.ExpiredDate > DateTime.Now && l.LotQuantity > 0)
                            .OrderBy(l => l.ExpiredDate)
                            .Select(l => new { l.LotQuantity, l.SalePrice })
                            .ToListAsync();

                        var remain = (decimal)newQty;
                        foreach (var lot in lots)
                        {
                            if (remain <= 0) break;
                            var take = Math.Min(remain, (decimal)lot.LotQuantity);
                            picked += take;
                            costSum += take * lot.SalePrice;
                            remain -= take;
                        }
                    }

                    d.UnitPrice = picked > 0 ? Math.Round(costSum / picked, 2, MidpointRounding.AwayFromZero) : 0m;
                    d.SubTotalPrice = Math.Round(d.UnitPrice * d.Quantity, 2, MidpointRounding.AwayFromZero);
                }

                so.TotalPrice = Math.Round(so.SalesOrderDetails.Sum(x => x.SubTotalPrice), 2, MidpointRounding.AwayFromZero);

                _unitOfWork.SalesOrder.Update(so);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Cập nhật số lượng bản nháp thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi UpdateDraftQuantitiesAsync({SalesOrderId})", salesOrderId);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi cập nhật bản nháp.",
                    Data = false
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
    }
}
