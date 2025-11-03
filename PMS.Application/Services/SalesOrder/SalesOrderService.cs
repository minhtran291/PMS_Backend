using AutoMapper;
using MailKit.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<ServiceResult<FEFOPlanResponseDTO>> BuildFefoPlanAsync(FEFOPlanRequestDTO request)
        {
            try
            {
                var quotation = await _unitOfWork.Quotation.Query()
                    .Include(q => q.QuotationDetails)
                    .FirstOrDefaultAsync(q => q.QID == request.QID);

                if (quotation == null)
                {
                    return new ServiceResult<FEFOPlanResponseDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy báo giá",
                        Data = null
                    };
                }

                var plan = new FEFOPlanResponseDTO { QID = quotation.QID };
                var lowStockList = new List<string>();

                foreach (var item in request.Items)
                {
                    var lots = await _unitOfWork.LotProduct.Query()
                        .Include(l => l.Supplier)
                        .Include(l => l.Product)
                        .Where(l => l.ProductID == item.ProductId && l.ExpiredDate > DateTime.Now)
                        .OrderBy(l => l.ExpiredDate)
                        .ToListAsync();

                    decimal remaining = item.Quantity;
                    var productPlan = new FEFOProductPlanDTO
                    {
                        ProductId = item.ProductId,
                        ProductName = lots.FirstOrDefault()?.Product.ProductName ?? "N/A",
                        RequestedQuantity = item.Quantity
                    };

                    foreach (var lot in lots)
                    {
                        if (remaining <= 0) break;
                        var available = lot.LotQuantity;
                        var pickQty = Math.Min(available, remaining);
                        remaining -= pickQty;

                        productPlan.Picks.Add(new LotPickDTO
                        {
                            Lot = new FEFOLotDTO
                            {
                                LotId = lot.LotID,
                                ProductId = lot.ProductID,
                                ProductName = lot.Product.ProductName,
                                InputDate = lot.InputDate,
                                ExpiredDate = lot.ExpiredDate,
                                AvailableQuantity = lot.LotQuantity,
                                UnitPrice = lot.SalePrice,
                                SupplierId = lot.SupplierID,
                                SupplierName = lot.Supplier.Name
                            },
                            PickQuantity = pickQty
                        });
                    }

                    if (remaining > 0)
                        lowStockList.Add($"{productPlan.ProductName} (thiếu {remaining})");

                    plan.Products.Add(productPlan);
                }

                // Thông báo thiếu hàng cho Purchases Staff
                if (lowStockList.Any())
                {
                    var msg = string.Join("; ", lowStockList);
                    await _noti.SendNotificationToRolesAsync(
                        "system",
                        ["PURCHASES_STAFF"],
                        "Thiếu hàng trong kho",
                        $"FEFO phát hiện sản phẩm thiếu: {msg}",
                        Core.Domain.Enums.NotificationType.Warning
                    );
                }

                return new ServiceResult<FEFOPlanResponseDTO>
                {
                    StatusCode = 200,
                    Message = "Tính FEFO thành công",
                    Data = plan
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính FEFO");
                return new ServiceResult<FEFOPlanResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Lỗi trong quá trình xử lý FEFO",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<bool>> ConfirmPaymentAsync(string salesOrderId, decimal amountVnd, string method, string? externalTxnId = null)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == salesOrderId);

                if (order == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng",
                        Data = false
                    };
                }

                await _unitOfWork.BeginTransactionAsync();

                if (amountVnd < order.OrderTotalPrice)
                {
                    order.Status = SalesOrderStatus.Deposited;
                    order.DepositAmount = amountVnd;
                }
                else
                {
                    order.Status = SalesOrderStatus.Paid;
                    order.DepositAmount = order.OrderTotalPrice;
                }

                _unitOfWork.SalesOrder.Update(order);

                // Giảm tồn kho
                foreach (var d in order.SalesOrderDetails)
                {
                    var lot = await _unitOfWork.LotProduct.Query().FirstOrDefaultAsync(l => l.LotID == d.LotId);
                    if (lot != null)
                    {
                        lot.LotQuantity -= (int) d.Quantity;
                        _unitOfWork.LotProduct.Update(lot);
                    }
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Xác nhận thanh toán đơn {salesOrderId} qua {method} thành công.");
                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Message = "Thanh toán thành công",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Xác nhận thanh toán thất bại");
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Message = "Lỗi xác nhận thanh toán",
                    Data = false
                };
            }
        }

        public async Task<ServiceResult<CreateOrderResponseDTO>> CreateOrderFromQuotationAsync(FEFOPlanRequestDTO request, string createdBy)
        {
            try
            {
                var fefo = await BuildFefoPlanAsync(request);
                if (fefo.Data == null)
                    return new ServiceResult<CreateOrderResponseDTO>
                    {
                        StatusCode = 400,
                        Message = "Không thể tạo đơn hàng chờ do dữ liệu FEFO không hợp lệ",
                        Data = null
                    };

                await _unitOfWork.BeginTransactionAsync();

                var order = new Core.Domain.Entities.SalesOrder
                {
                    SalesQuotationId = request.QID,
                    CreateBy = createdBy,
                    CreateAt = DateTime.Now,
                    Status = SalesOrderStatus.Pending,
                    OrderTotalPrice = fefo.Data.Products.Sum(p => p.Picks.Sum(pk => pk.PickQuantity * pk.Lot.UnitPrice)),
                    DepositAmount = 0
                };

                await _unitOfWork.SalesOrder.AddAsync(order);
                await _unitOfWork.CommitAsync();

                foreach (var product in fefo.Data.Products)
                {
                    foreach (var pick in product.Picks)
                    {
                        await _unitOfWork.SalesOrderDetails.AddAsync(new SalesOrderDetails
                        {
                            SalesOrderId = order.OrderId,
                            LotId = pick.Lot.LotId,
                            Quantity = pick.PickQuantity,
                            UnitPrice = pick.Lot.UnitPrice
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<CreateOrderResponseDTO>
                {
                    StatusCode = 200,
                    Message = "Tạo đơn hàng chờ thành công",
                    Data = new CreateOrderResponseDTO
                    {
                        OrderId = order.OrderId,
                        QID = request.QID,
                        OrderTotalPrice = order.OrderTotalPrice,
                        CreatedAt = order.CreateAt,
                        PlanUsed = fefo.Data
                    }
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Tạo đơn hàng chờ thất bại");
                return new ServiceResult<CreateOrderResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi tạo đơn hàng chờ",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<VnPayInitResponseDTO>> GenerateVnPayPaymentAsync(string orderId, string paymentType)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query().FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    return new ServiceResult<VnPayInitResponseDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng",
                        Data = null
                    };
                }

                decimal amount = paymentType.ToLower() == "deposit"
                    ? (order.OrderTotalPrice / 10)   // Deposit 10%
                    : order.OrderTotalPrice;

                var info = paymentType == "deposit" ? "Thanh toán tiền cọc" : "Thanh toán toàn bộ đơn hàng";

                var paymentUrl = _vnPay.CreatePaymentUrl(orderId, (long)amount, info);
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

        public async Task<ServiceResult<object>> GetOrderDetailsAsync(string orderId)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(d => d.Lot)
                            .ThenInclude(l => l.Product)
                    .Include(o => o.SalesOrderDetails)
                        .ThenInclude(d => d.Lot)
                            .ThenInclude(l => l.Supplier)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

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
                    order.OrderId,
                    order.SalesQuotationId,
                    order.CreateBy,
                    order.CreateAt,
                    order.Status,
                    order.OrderTotalPrice,
                    order.DepositAmount,
                    Details = order.SalesOrderDetails.Select(d => new
                    {
                        d.LotId,
                        d.Quantity,
                        d.UnitPrice,
                        ProductName = d.Lot.Product.ProductName,
                        SupplierName = d.Lot.Supplier.Name,
                        ExpiredDate = d.Lot.ExpiredDate
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

        public async Task<ServiceResult<List<QuotationProductDTO>>> GetQuotationProductsAsync(int qid)
        {
            try
            {
                var quotation = await _unitOfWork.Quotation.Query()
                    .Include(q => q.QuotationDetails)
                    .FirstOrDefaultAsync(q => q.QID == qid);

                if (quotation == null)
                {
                    return new ServiceResult<List<QuotationProductDTO>>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy báo giá",
                        Data = null
                    };
                }

                var data = quotation.QuotationDetails.Select(d => new QuotationProductDTO
                {
                    ProductId = d.ProductID,
                    ProductName = d.ProductName,
                    ProductDescription = d.ProductDescription,
                    ProductUnit = d.ProductUnit,
                    UnitPrice = d.UnitPrice,
                    ProductDate = d.ProductDate
                }).ToList();

                return new ServiceResult<List<QuotationProductDTO>>
                {
                    StatusCode = 200,
                    Message = "Lấy danh sách sản phẩm trong báo giá thành công",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy sản phẩm báo giá");
                return new ServiceResult<List<QuotationProductDTO>>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra",
                    Data = null
                };
            }
        }
    }
}
