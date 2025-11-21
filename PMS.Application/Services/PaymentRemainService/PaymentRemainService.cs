using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Services.Base;
using PMS.Application.Services.PaymentRemainService;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;
using PMS.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Application.DTOs.PaymentRemain;

namespace PMS.Application.Services.PaymentRemainService
{
    public class PaymentRemainService (IUnitOfWork unitOfWork,
        IMapper mapper, 
        ILogger<PaymentRemainService> logger) : Service(unitOfWork, mapper), IPaymentRemainService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<PaymentRemainService> _logger = logger;


        public async Task<ServiceResult<Core.Domain.Entities.PaymentRemain>> 
            CreatePaymentRemainForGoodsIssueNoteAsync(int goodsIssueNoteId)
        {
            try
            {
                // Lấy phiếu xuất + SalesOrder + SalesOrderDetails + GoodsIssueNoteDetails + SalesQuotation (để lấy DepositPercent)
                var note = await _unitOfWork.GoodsIssueNote.Query()
                    .Include(g => g.StockExportOrder)
                        .ThenInclude(seo => seo.SalesOrder)
                            .ThenInclude(o => o.SalesOrderDetails)
                    .Include(g => g.StockExportOrder)
                        .ThenInclude(seo => seo.SalesOrder)
                            .ThenInclude(o => o.SalesQuotation)
                    .Include(g => g.GoodsIssueNoteDetails)
                    .FirstOrDefaultAsync(g => g.Id == goodsIssueNoteId);

                if (note == null)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy phiếu xuất.",
                        Data = null
                    };
                }

                if (note.StockExportOrder == null || note.StockExportOrder.SalesOrder == null)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 400,
                        Message = "Phiếu xuất chưa gắn với đơn hàng.",
                        Data = null
                    };
                }

                var order = note.StockExportOrder.SalesOrder;

                // Nếu đã có PaymentRemain gắn với phiếu này rồi thì không tạo lại
                var existedPayment = await _unitOfWork.PaymentRemains.Query()
                    .FirstOrDefaultAsync(p => p.GoodsIssueNoteId == note.Id
                                              && p.Status != PaymentStatus.Failed);

                if (existedPayment != null)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 400,
                        Message = "Đã tồn tại yêu cầu thanh toán cho phiếu xuất này.",
                        Data = null
                    };
                }

                // 1) Tổng tiền đơn hàng
                var orderTotal = order.TotalPrice;
                if (orderTotal <= 0)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 400,
                        Message = "Giá trị đơn hàng không hợp lệ.",
                        Data = null
                    };
                }

                // 2) Giá trị phiếu xuất (theo LotId)
                decimal noteAmount = 0m;
                foreach (var d in note.GoodsIssueNoteDetails)
                {
                    var soDetail = order.SalesOrderDetails.FirstOrDefault(x => x.LotId == d.LotId);
                    if (soDetail == null)
                    {
                        return new ServiceResult<PaymentRemain>
                        {
                            StatusCode = 400,
                            Message = $"Không tìm thấy SalesOrderDetails cho LotId={d.LotId}.",
                            Data = null
                        };
                    }

                    noteAmount += soDetail.UnitPrice * d.Quantity;
                }

                if (noteAmount <= 0)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 400,
                        Message = "Giá trị phiếu xuất không hợp lệ.",
                        Data = null
                    };
                }

                // 3) Tổng cọc theo % ở SalesQuotation (không lấy từ PaymentRemain)
                if (order.SalesQuotation == null)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng chưa gắn với báo giá, không xác định được tỷ lệ cọc.",
                        Data = null
                    };
                }

                var depositFixed = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                // Bảo vệ: nếu PaidAmount < depositFixed thì coi như chưa cọc đủ
                if (order.PaidAmount < depositFixed)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 400,
                        Message = "Đơn hàng chưa thanh toán đủ tiền cọc, không thể tạo thanh toán phần còn lại.",
                        Data = null
                    };
                }

                // 4) Tỷ lệ phiếu xuất trên đơn hàng
                var proportion = noteAmount / orderTotal;

                // 5) Phần cọc chia cho phiếu này
                var allocatedDeposit = decimal.Round(
                    depositFixed * proportion,
                    0,
                    MidpointRounding.AwayFromZero);

                // 6) Các khoản Remain/Full đã trả cho phiếu này trước đó
                var paidForThisNote = await _unitOfWork.PaymentRemains.Query()
                    .Where(p => p.GoodsIssueNoteId == note.Id
                                && p.Status == PaymentStatus.Success
                                && (p.PaymentType == PaymentType.Remain || p.PaymentType == PaymentType.Full))
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                // 7) Số tiền cần thanh toán còn lại cho phiếu này
                var amountDueForNote = noteAmount - allocatedDeposit - paidForThisNote;
                if (amountDueForNote <= 0)
                {
                    return new ServiceResult<PaymentRemain>
                    {
                        StatusCode = 400,
                        Message = "Phiếu xuất này đã được cấn trừ đủ bởi tiền cọc và các lần thanh toán trước.",
                        Data = null
                    };
                }

                await _unitOfWork.BeginTransactionAsync();

                var payment = new PaymentRemain
                {
                    SalesOrderId = order.SalesOrderId,
                    GoodsIssueNoteId = note.Id,
                    PaymentType = PaymentType.Remain,
                    PaymentMethod = PaymentMethod.None, 
                    Amount = amountDueForNote,
                    CreateRequestAt = DateTime.Now,
                    Status = PaymentStatus.Pending,
                    Gateway = null
                };


                await _unitOfWork.PaymentRemains.AddAsync(payment);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<PaymentRemain>
                {
                    StatusCode = 201,
                    Message = "Tạo yêu cầu thanh toán phần còn lại thành công.",
                    Data = payment
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex,
                    "Lỗi CreatePaymentRemainForGoodsIssueNoteAsync({GoodsIssueNoteId})",
                    goodsIssueNoteId);

                return new ServiceResult<PaymentRemain>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi tạo yêu cầu thanh toán.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<PaymentRemainItemDTO>> GetPaymentRemainDetailAsync(int id)
        {
            try
            {
                var entity = await _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.SalesQuotation)
                            .ThenInclude(sq => sq.SalesQuotaionDetails)
                                .ThenInclude(sqd => sqd.TaxPolicy)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.SalesOrderDetails)
                    .Include(p => p.GoodsIssueNote)
                        .ThenInclude(g => g.Warehouse)
                    .Include(p => p.GoodsIssueNote)
                        .ThenInclude(g => g.GoodsIssueNoteDetails)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (entity == null)
                {
                    return new ServiceResult<PaymentRemainItemDTO>
                    {
                        StatusCode = 404,
                        Success = false,
                        Message = "Không tìm thấy PaymentRemain.",
                        Data = null
                    };
                }

                var so = entity.SalesOrder;
                var customer = so.Customer;
                var quotation = so.SalesQuotation;
                var note = entity.GoodsIssueNote;

                decimal depositPercent = quotation?.DepositPercent ?? 0m;
                decimal depositAmount = Math.Round(
                    so.TotalPrice * (depositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                decimal remainingAmount = so.TotalPrice - so.PaidAmount;

                var details = new List<PaymentRemainGoodsIssueDetailDTO>();

                if (note != null)
                {
                    int index = 1;

                    foreach (var d in note.GoodsIssueNoteDetails)
                    {
                        // 1) Tìm SalesOrderDetail theo LotId
                        var soDetail = so.SalesOrderDetails
                            .FirstOrDefault(x => x.LotId == d.LotId);

                        if (soDetail == null)
                        {
                            // Có thể log cảnh báo nếu cần
                            continue;
                        }

                        // 2) Tìm SalesQuotationDetail tương ứng theo ProductId
                        var sqDetail = quotation?.SalesQuotaionDetails
                            .FirstOrDefault(x => x.ProductId == soDetail.LotProduct.Product.ProductID); 

                        if (sqDetail == null)
                        {
                            var fallbackProductName = soDetail.LotProduct.Product.ProductName; 

                            details.Add(new PaymentRemainGoodsIssueDetailDTO
                            {
                                Index = index++,
                                ProductName = fallbackProductName,
                                Quantity = d.Quantity,
                                UnitPrice = soDetail.UnitPrice,
                                TaxPercent = 0,
                                UnitPriceAfterTax = soDetail.UnitPrice,
                                ExpiredDate = soDetail.SalesOrder.SalesOrderExpiredDate, 
                                SubTotal = soDetail.UnitPrice * d.Quantity,
                                SubTotalAfterTax = soDetail.UnitPrice * d.Quantity
                            });

                            continue;
                        }

                        var taxPercent = sqDetail.TaxPolicy?.Rate ?? 0m; 

                        // 4) Lấy các thông tin hiển thị
                        var productName = sqDetail.Product.ProductName;   
                        var unitPrice = soDetail.UnitPrice;    
                        var expiredDate = soDetail.SalesOrder.SalesOrderExpiredDate;   

                        var unitPriceAfterTax = unitPrice * (1 + taxPercent / 100m);
                        var subTotal = unitPrice * d.Quantity;
                        var subTotalAfterTax = unitPriceAfterTax * d.Quantity;

                        details.Add(new PaymentRemainGoodsIssueDetailDTO
                        {
                            Index = index++,
                            ProductName = productName,
                            Quantity = d.Quantity,
                            UnitPrice = unitPrice,
                            TaxPercent = taxPercent,
                            UnitPriceAfterTax = unitPriceAfterTax,
                            ExpiredDate = expiredDate,
                            SubTotal = subTotal,
                            SubTotalAfterTax = subTotalAfterTax
                        });
                    }
                }

                var dto = new PaymentRemainItemDTO
                {
                    Id = entity.Id,
                    SalesOrderId = entity.SalesOrderId,
                    GoodsIssueNoteId = entity.GoodsIssueNoteId,
                    PaymentType = entity.PaymentType,
                    PaymentMethod = entity.PaymentMethod,
                    Status = entity.Status,
                    Amount = entity.Amount,
                    PaidAt = entity.CreateRequestAt,    
                    GatewayTransactionRef = entity.GatewayTransactionRef,
                    Gateway = entity.Gateway,

                    SalesOrderCode = so.SalesOrderCode,
                    SalesOrderTotalPrice = so.TotalPrice,
                    SalesOrderPaidAmount = so.PaidAmount,

                    CustomerId = customer?.Id,
                    CustomerName = customer?.FullName,
                    RequestCreatedAt = entity.CreateRequestAt,
                    PaymentStatusText = entity.Status.ToString(),

                    GoodsIssueNoteCode = note?.GoodsIssueNoteCode,
                    GoodsIssueNoteCreatedAt = note?.CreateAt,

                    DepositAmount = depositAmount,
                    DepositPercent = depositPercent,
                    RemainingAmount = remainingAmount,

                    GoodsIssueDetails = details
                };

                return new ServiceResult<PaymentRemainItemDTO>
                {
                    StatusCode = 200,
                    Success = true,
                    Message = "Lấy thông tin chi tiết PaymentRemain thành công.",
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetPaymentRemainDetailAsync({Id})", id);

                return new ServiceResult<PaymentRemainItemDTO>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy chi tiết PaymentRemain.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<List<int>>> GetPaymentRemainIdsBySalesOrderIdAsync(int salesOrderId)
        {
            try
            {
                var ids = await _unitOfWork.PaymentRemains.Query()
                    .Where(p => p.SalesOrderId == salesOrderId
                                && p.Status == PaymentStatus.Success
                                && (p.PaymentType == PaymentType.Remain
                                    || p.PaymentType == PaymentType.Full))
                    .OrderBy(p => p.CreateRequestAt)
                    .Select(p => p.Id)
                    .ToListAsync();

                return new ServiceResult<List<int>>
                {
                    StatusCode = 200,
                    Success = true,
                    Message = "Lấy danh sách PaymentRemainId thành công.",
                    Data = ids
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi GetPaymentRemainIdsBySalesOrderIdAsync({SalesOrderId})",
                    salesOrderId);

                return new ServiceResult<List<int>>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách PaymentRemainId.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<List<PaymentRemainItemDTO>>> GetPaymentRemainsAsync(PaymentRemainListRequestDTO request)
        {
            try
            {
                var query = _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.SalesOrder)
                    .AsNoTracking();

                //Filter theo CustomerId
                if (request.CustomerId != null)
                {
                    query = query.Where(p => p.SalesOrder.CreateBy == request.CustomerId);
                }

                // Filter theo SalesOrderId
                if (request.SalesOrderId.HasValue)
                {
                    query = query.Where(p => p.SalesOrderId == request.SalesOrderId.Value);
                }

                // Filter theo GoodsIssueNoteId
                if (request.GoodsIssueNoteId.HasValue)
                {
                    query = query.Where(p => p.GoodsIssueNoteId == request.GoodsIssueNoteId.Value);
                }

                // Filter theo Status
                if (request.Status.HasValue)
                {
                    query = query.Where(p => p.Status == request.Status.Value);
                }

                // Filter theo PaymentMethod
                if (request.PaymentMethod.HasValue)
                {
                    query = query.Where(p => p.PaymentMethod == request.PaymentMethod.Value);
                }

                // Filter theo PaymentType
                if (request.PaymentType.HasValue)
                {
                    query = query.Where(p => p.PaymentType == request.PaymentType.Value);
                }

                query = query.OrderByDescending(p => p.CreateRequestAt)
                             .ThenByDescending(p => p.Id);

                var items = await query.ToListAsync();

                var data = items.Select(p => new PaymentRemainItemDTO
                {
                    Id = p.Id,
                    SalesOrderId = p.SalesOrderId,
                    GoodsIssueNoteId = p.GoodsIssueNoteId,
                    PaymentType = p.PaymentType,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    Amount = p.Amount,
                    PaidAt = p.CreateRequestAt,
                    GatewayTransactionRef = p.GatewayTransactionRef,
                    Gateway = p.Gateway,
                    SalesOrderCode = p.SalesOrder?.SalesOrderCode,
                    SalesOrderTotalPrice = p.SalesOrder.TotalPrice,
                    SalesOrderPaidAmount = p.SalesOrder.PaidAmount

                }).ToList();

                return new ServiceResult<List<PaymentRemainItemDTO>>
                {
                    StatusCode = 200,
                    Success = true,
                    Message = "Lấy danh sách PaymentRemain thành công.",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetPaymentRemainsAsync");
                return new ServiceResult<List<PaymentRemainItemDTO>>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách PaymentRemain.",
                    Data = null
                };
            }
        }


    }
}
