using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.DTOs.VnPay;
using PMS.Application.Services.Base;
using PMS.Application.Services.Notification;
using PMS.Application.Services.VNpay;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.SalesOrder
{
    public class SalesOrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SalesOrderService> logger,
        INotificationService notificationService,
        IVnPayService vnPayService) : Service(unitOfWork, mapper) //ISalesOrderService
    {
        private readonly ILogger<SalesOrderService> _logger = logger;
        private readonly INotificationService _noti = notificationService;
        private readonly IVnPayService _vnPay = vnPayService;
        private readonly IMapper _mapper = mapper;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;


        //public async Task<ServiceResult<bool>> ConfirmPaymentAsync(int salesOrderId)
        //{
        //    try
        //    {
        //        var order = await _unitOfWork.SalesOrder.Query()
        //            .FirstOrDefaultAsync(o => o.OrderId == salesOrderId);

        //        if (order == null)
        //        {
        //            return new ServiceResult<bool>
        //            {
        //                StatusCode = 404,
        //                Message = "Không tìm thấy đơn hàng",
        //                Data = false
        //            };
        //        }

        //        if (order.Status != SalesOrderStatus.Send)
        //        {
        //            return new ServiceResult<bool>
        //            {
        //                StatusCode = 400,
        //                Message = "Chỉ xác nhận thanh toán cho đơn ở trạng thái đã được gửi đi.",
        //                Data = false
        //            };
        //        }

        //        order.Status = SalesOrderStatus.Paid;
        //        order.DepositAmount = order.OrderTotalPrice;

        //        _unitOfWork.SalesOrder.Update(order);
        //        await _unitOfWork.CommitAsync();

        //        return new ServiceResult<bool>
        //        {
        //            StatusCode = 200,
        //            Message = "Xác nhận thanh toán thành công.",
        //            Data = true
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi ConfirmPaymentAsync");
        //        return new ServiceResult<bool>
        //        {
        //            StatusCode = 500,
        //            Message = "Lỗi xác nhận thanh toán",
        //            Data = false
        //        };
        //    }
        //}

        public async Task<ServiceResult<object>> CreateDraftFromSalesQuotationAsync(int salesQuotationId, string createdBy)
        {
            try
            {
                var sq = await _unitOfWork.SalesQuotation.Query()
                    .Include(x => x.SalesQuotaionDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(x => x.Id == salesQuotationId);

                if (sq == null)
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy SalesQuotation",
                        Data = null
                    };
                }

                var so = new Core.Domain.Entities.SalesOrder
                {
                    SalesQuotationId = sq.Id,
                    CreateBy = createdBy,
                    CreateAt = DateTime.Now,
                    Status = SalesOrderStatus.Draft
                };

                await _unitOfWork.SalesOrder.AddAsync(so);
                await _unitOfWork.CommitAsync();

                foreach (var qd in sq.SalesQuotaionDetails)
                {
                    var sod = new SalesOrderDetails
                    {
                        SalesOrderId = so.SalesOrderId,
                        ProductId = qd.ProductId,
                        Quantity = 0
                    };
                    await _unitOfWork.SalesOrderDetails.AddAsync(sod);
                }
                await _unitOfWork.CommitAsync();

                var data = new
                {
                    so.SalesOrderId,
                    so.SalesQuotationId,
                    so.CreateBy,
                    so.CreateAt,
                    so.Status,
                    Details = sq.SalesQuotaionDetails.Select(d => new
                    {
                        d.ProductId,
                        ProductName = d.Product.ProductName,
                        Quantity = 0,
                        UnitPrice = d.SalesPrice
                    }).ToList()
                };

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo SalesOrder Draft thành công",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi CreateDraftFromSalesQuotationAsync");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi tạo bản nháp đơn hàng",
                    Data = null
                };
            }
        }



        public async Task<ServiceResult<bool>> DeleteDraftAsync(int orderId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

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

        //public async Task<ServiceResult<VnPayInitResponseDTO>> GenerateVnPayPaymentAsync(string orderId, string paymentType)
        //{
        //    try
        //    {
        //        var order = await _unitOfWork.SalesOrder.Query().FirstOrDefaultAsync(o => o.OrderId == orderId);
        //        if (order == null)
        //        {
        //            return new ServiceResult<VnPayInitResponseDTO>
        //            {
        //                StatusCode = 404,
        //                Message = "Không tìm thấy đơn hàng",
        //                Data = null
        //            };
        //        }

        //        if (order.Status != SalesOrderStatus.Send)
        //            return new ServiceResult<VnPayInitResponseDTO> { StatusCode = 400, Message = "Chỉ khởi tạo thanh toán cho đơn ở trạng thái đã gửi." };


        //        decimal amount = paymentType.ToLower() == "deposit"
        //            ? (order.OrderTotalPrice / 10)   // Deposit 10%
        //            : order.OrderTotalPrice;

        //        var info = paymentType == "deposit" ? "Thanh toán tiền cọc" : "Thanh toán toàn bộ đơn hàng";

        //        var paymentUrl = _vnPay.CreatePaymentUrl(orderId, (long)amount, info);
        //        var qrData = _vnPay.GenerateQrDataUrl(paymentUrl);

        //        return new ServiceResult<VnPayInitResponseDTO>
        //        {
        //            StatusCode = 200,
        //            Message = "Khởi tạo thanh toán VNPay thành công",
        //            Data = new VnPayInitResponseDTO
        //            {
        //                PaymentUrl = paymentUrl,
        //                QrDataUrl = qrData,
        //                Amount = amount,
        //                PaymentType = paymentType
        //            }
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi khởi tạo thanh toán VNPay");
        //        return new ServiceResult<VnPayInitResponseDTO>
        //        {
        //            StatusCode = 500,
        //            Message = "Lỗi trong quá trình tạo thanh toán VNPay",
        //            Data = null
        //        };
        //    }
        //}

        public async Task<ServiceResult<object>> GetOrderDetailsAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(l => l.Product)
                    .Include(q => q.SalesQuotation)
                        .ThenInclude(d => d.SalesQuotaionDetails)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

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

        public async Task<ServiceResult<IEnumerable<object>>> ListOrdersAsync(string userId)
        {
            try
            {
                var orders = await _unitOfWork.SalesOrder.Query()
                    .Where(o => o.CreateBy == userId)
                    .OrderByDescending(o => o.CreateAt)
                    .Select(o => new
                    {
                        o.SalesOrderId,
                        o.Status,
                        o.TotalPrice,
                        //o.DepositAmount,
                        o.CreateAt
                    })
                    .ToListAsync();

                return new ServiceResult<IEnumerable<object>>
                {
                    StatusCode = 200,
                    Message = "Lấy danh sách đơn hàng thành công",
                    Data = orders
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ListOrdersAsync");
                return new ServiceResult<IEnumerable<object>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi lấy danh sách đơn hàng",
                    Data = null
                };
            }
        }

        ////Customer mark order is complete
        public async Task<ServiceResult<bool>> MarkCompleteAsync(int orderId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

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
        public async Task<ServiceResult<object>> SendOrderAsync(int orderId)
        {
            try
            {
                var so = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

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

        //// customer update sales order when status is draft
        //public async Task<ServiceResult<bool>> UpdateDraftQuantitiesAsync(int orderId, List<DraftSalesOrderDTO> items)
        //{
        //    try
        //    {
        //        var so = await _unitOfWork.SalesOrder.Query()
        //            .Include(o => o.SalesOrderDetails)
        //            .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

        //        if (so == null)
        //        {
        //            return new ServiceResult<bool>
        //            {
        //                StatusCode = 404,
        //                Message = "Không tìm thấy SalesOrder",
        //                Data = false
        //            };
        //        }

        //        if (so.Status != SalesOrderStatus.Draft)
        //        {
        //            return new ServiceResult<bool>
        //            {
        //                StatusCode = 400,
        //                Message = "Chỉ được cập nhật số lượng khi đơn ở trạng thái Draft",
        //                Data = false
        //            };
        //        }

        //        if (items == null || items.Count == 0)
        //        {
        //            return new ServiceResult<bool>
        //            {
        //                StatusCode = 400,
        //                Message = "Danh sách cập nhật rỗng",
        //                Data = false
        //            };
        //        }

        //        var mapQty = items.ToDictionary(x => x.ProductId, x => x.Quantity);

        //        foreach (var d in so.SalesOrderDetails)
        //        {
        //            if (d.ProductId != null && mapQty.TryGetValue(d.ProductId, out var newQty))
        //            {
        //                d.Quantity = newQty < 0 ? 0 : newQty;
        //                decimal remain = d.Quantity;
        //                decimal accPrice = d.UnitPrice;

        //                var lots = await _unitOfWork.LotProduct.Query()
        //                    .Where(l => l.ProductID == d.ProductId && l.ExpiredDate > DateTime.Now && l.LotQuantity > 0)
        //                    .OrderBy(l => l.ExpiredDate)
        //                    .Select(l => new { l.LotQuantity, l.SalePrice })
        //                    .ToListAsync();

        //                foreach (var lot in lots)
        //                {
        //                    if (remain <= 0) break;
        //                    var pick = Math.Min(remain, lot.LotQuantity);
        //                    accPrice += pick * lot.SalePrice;
        //                    remain -= pick;
        //                }

        //                d.UnitPrice = (d.Quantity > 0)
        //                    ? (accPrice / Math.Max(d.Quantity, 1))
        //                    : 0m;
        //            }
        //        }

        //        so.TotalPrice = so.SalesOrderDetails.Sum(x => x.UnitPrice * x.Quantity);

        //        _unitOfWork.SalesOrder.Update(so);
        //        await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Cập nhật số lượng bản nháp thành công",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi UpdateDraftQuantitiesAsync");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Có lỗi khi cập nhật số lượng bản nháp",
                    Data = false
                };
            }
        }

        //Generate SalesOrderId
        private static string GenerateSalesOrderId()
            => $"SO{DateTime.Now:yyyyMMddHHmmssfff}";

    }
}
