using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.PaymentRemain;
using PMS.Application.DTOs.VnPay;
using PMS.Application.Services.Base;
using PMS.Application.Services.PaymentRemainService;
using PMS.Application.Services.SalesOrder;
using PMS.Application.Services.VNpay;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.PaymentRemainService
{
    public class PaymentRemainService (IUnitOfWork unitOfWork,
        IMapper mapper, 
        ILogger<PaymentRemainService> logger,
        IVnPayService vnPayService) : Service(unitOfWork, mapper), IPaymentRemainService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<PaymentRemainService> _logger = logger;
        private readonly IVnPayService _vnPayService = vnPayService;

        public async Task<ServiceResult<PaymentRemainItemDTO>> CreatePaymentRemainForInvoiceAsync(CreatePaymentRemainRequestDTO request)
        {
            try
            {
                if (request == null)
                {
                    return new ServiceResult<PaymentRemainItemDTO>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "Payload trống.",
                        Data = null
                    };
                }

                if (request.InvoiceId <= 0)
                {
                    return new ServiceResult<PaymentRemainItemDTO>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "InvoiceId là bắt buộc.",
                        Data = null
                    };
                }

                // Lấy Invoice + SalesOrder + PaymentRemains
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.PaymentRemains)
                    .Include(i => i.PaymentRemains)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId);

                if (invoice == null)
                {
                    return new ServiceResult<PaymentRemainItemDTO>
                    {
                        StatusCode = 404,
                        Success = false,
                        Message = "Không tìm thấy hóa đơn.",
                        Data = null
                    };
                }

                var order = invoice.SalesOrder;
                if (order == null)
                {
                    return new ServiceResult<PaymentRemainItemDTO>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "Hóa đơn chưa gắn với đơn hàng.",
                        Data = null
                    };
                }

                // Tính lại tổng đã thanh toán cho invoice dựa trên PaymentRemain
                var remainPaid = invoice.PaymentRemains
                    .Where(p => p.PaidAt != null)
                    .Sum(p => p.Amount);

                var totalPaid = invoice.TotalDeposit + remainPaid;
                var totalRemain = invoice.TotalAmount - totalPaid;

                if (totalRemain <= 0)
                {
                    return new ServiceResult<PaymentRemainItemDTO>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "Hóa đơn này đã được thanh toán đủ.",
                        Data = null
                    };
                }

                // Số tiền lần này
                decimal amountToPay;
                if (request.Amount.HasValue)
                {
                    amountToPay = request.Amount.Value;
                    if (amountToPay <= 0)
                    {
                        return new ServiceResult<PaymentRemainItemDTO>
                        {
                            StatusCode = 400,
                            Success = false,
                            Message = "Số tiền thanh toán phải lớn hơn 0.",
                            Data = null
                        };
                    }

                    if (amountToPay > totalRemain)
                    {
                        return new ServiceResult<PaymentRemainItemDTO>
                        {
                            StatusCode = 400,
                            Success = false,
                            Message = "Số tiền thanh toán vượt quá số tiền còn lại của hóa đơn.",
                            Data = null
                        };
                    }
                }
                else
                {
                    // Nếu không truyền Amount, mặc định thanh toán hết phần còn lại
                    amountToPay = totalRemain;
                }

                var now = DateTime.Now;

                var payment = new PaymentRemain
                {
                    InvoiceId = invoice.Id,
                    InvoiceCode = invoice.InvoiceCode,
                    SalesOrderId = order.SalesOrderId,

                    PaymentType = request.PaymentType,
                    PaymentMethod = request.PaymentMethod,

                    Amount = amountToPay,
                    CreateRequestAt = now,
                    PaidAt = null, // sẽ set sau khi thanh toán thành công

                    VNPayStatus = VNPayStatus.Pending,
                    Gateway = request.PaymentMethod == PaymentMethod.VnPay ? "VNPAY" : null,
                    GatewayTransactionRef = null
                };

                await _unitOfWork.PaymentRemains.AddAsync(payment);
                await _unitOfWork.CommitAsync();

                var dto = new PaymentRemainItemDTO
                {
                    Id = payment.Id,
                    InvoiceId = payment.InvoiceId,
                    InvoiceCode = payment.InvoiceCode,
                    SalesOrderId = payment.SalesOrderId,
                    SalesOrderCode = order.SalesOrderCode,
                    PaymentType = payment.PaymentType,
                    PaymentMethod = payment.PaymentMethod,
                    VNPayStatus = payment.VNPayStatus,
                    Amount = payment.Amount,
                    RequestCreatedAt = payment.CreateRequestAt,
                    PaidAt = payment.PaidAt,
                    Gateway = payment.Gateway,
                    GatewayTransactionRef = payment.GatewayTransactionRef,
                    SalesOrderTotalPrice = order.TotalPrice,
                    SalesOrderPaidAmount = order.PaidAmount,
                    CustomerId = order.CreateBy,
                    CustomerName = order.Customer?.FullName,
                    PaymentStatusText = $"{payment.PaymentMethod} - {payment.VNPayStatus}"
                };

                return new ServiceResult<PaymentRemainItemDTO>
                {
                    StatusCode = 201,
                    Success = true,
                    Message = "Tạo yêu cầu thanh toán cho hóa đơn thành công.",
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi CreatePaymentRemainForInvoiceAsync({InvoiceId})",
                    request?.InvoiceId);

                return new ServiceResult<PaymentRemainItemDTO>
                {
                    StatusCode = 500,
                    Success = false,
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
                    .Include(p => p.Invoice)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.Customer)
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
                var invoice = entity.Invoice;

                var dto = new PaymentRemainItemDTO
                {
                    Id = entity.Id,
                    InvoiceId = entity.InvoiceId,
                    InvoiceCode = entity.InvoiceCode,
                    SalesOrderId = entity.SalesOrderId,
                    SalesOrderCode = so?.SalesOrderCode,
                    PaymentType = entity.PaymentType,
                    PaymentMethod = entity.PaymentMethod,
                    VNPayStatus = entity.VNPayStatus,
                    Amount = entity.Amount,
                    RequestCreatedAt = entity.CreateRequestAt,
                    PaidAt = entity.PaidAt,
                    Gateway = entity.Gateway,
                    GatewayTransactionRef = entity.GatewayTransactionRef,

                    SalesOrderTotalPrice = so?.TotalPrice ?? 0m,
                    SalesOrderPaidAmount = so?.PaidAmount ?? 0m,

                    CustomerId = so?.CreateBy,
                    CustomerName = so?.Customer?.FullName,

                    PaymentStatusText = $"{entity.PaymentMethod} - {entity.VNPayStatus}"
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
                                && p.PaidAt != null
                                && p.VNPayStatus == VNPayStatus.Success)
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
                        .ThenInclude(so => so.Customer)
                    .Include(p => p.Invoice)
                    .AsNoTracking();

                // Filter theo CustomerId (CreateBy)
                if (!string.IsNullOrWhiteSpace(request.CustomerId))
                {
                    query = query.Where(p => p.SalesOrder.CreateBy == request.CustomerId);
                }

                // Filter theo SalesOrderId
                if (request.SalesOrderId.HasValue)
                {
                    query = query.Where(p => p.SalesOrderId == request.SalesOrderId.Value);
                }

                // Filter theo InvoiceId
                if (request.InvoiceId.HasValue)
                {
                    query = query.Where(p => p.InvoiceId == request.InvoiceId.Value);
                }

                // Filter theo VNPayStatus
                if (request.Status.HasValue)
                {
                    query = query.Where(p => p.VNPayStatus == request.Status.Value);
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

                var data = items.Select(p =>
                {
                    var so = p.SalesOrder;
                    return new PaymentRemainItemDTO
                    {
                        Id = p.Id,
                        InvoiceId = p.InvoiceId,
                        InvoiceCode = p.InvoiceCode,
                        SalesOrderId = p.SalesOrderId,
                        SalesOrderCode = so?.SalesOrderCode,
                        PaymentType = p.PaymentType,
                        PaymentMethod = p.PaymentMethod,
                        VNPayStatus = p.VNPayStatus,
                        Amount = p.Amount,
                        RequestCreatedAt = p.CreateRequestAt,
                        PaidAt = p.PaidAt,
                        GatewayTransactionRef = p.GatewayTransactionRef,
                        Gateway = p.Gateway,
                        SalesOrderTotalPrice = so?.TotalPrice ?? 0m,
                        SalesOrderPaidAmount = so?.PaidAmount ?? 0m,
                        CustomerId = so?.CreateBy,
                        CustomerName = so?.Customer?.FullName,
                        PaymentStatusText = $"{p.PaymentMethod} - {p.VNPayStatus}"
                    };
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

        public async Task<ServiceResult<VnPayInitResponseDTO>> InitVnPayForInvoiceAsync(int invoiceId, decimal? amount, string clientIp, string? locale = "vn")
        {
            try
            {
                // 1) Tạo PaymentRemain cho Invoice
                var createResult = await CreatePaymentRemainForInvoiceAsync(
                    new CreatePaymentRemainRequestDTO
                    {
                        InvoiceId = invoiceId,
                        Amount = amount,
                        PaymentMethod = PaymentMethod.VnPay,
                        PaymentType = PaymentType.Remain
                    });

                if (!createResult.Success || createResult.Data == null)
                {
                    return new ServiceResult<VnPayInitResponseDTO>
                    {
                        StatusCode = createResult.StatusCode,
                        Success = false,
                        Message = createResult.Message,
                        Data = null
                    };
                }

                var pr = createResult.Data;

                var vnPayReq = new VnPayInitRequestDTO
                {
                    PaymentType = "remain",
                    PaymentRemainId = pr.Id,
                    Locale = "vn"
                };

                var vnPayResult = await _vnPayService.InitVnPayAsync(vnPayReq, clientIp);

                return vnPayResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi InitVnPayForInvoiceAsync({InvoiceId})",
                    invoiceId);

                return new ServiceResult<VnPayInitResponseDTO>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Có lỗi khi khởi tạo VNPay cho hóa đơn.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<bool>> MarkPaymentSuccessAsync(int paymentRemainId, string? gatewayTransactionRef = null)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRemains.Query()
                    .FirstOrDefaultAsync(p => p.Id == paymentRemainId);

                if (payment == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Success = false,
                        Message = "Không tìm thấy PaymentRemain.",
                        Data = false
                    };
                }

                if (payment.PaidAt != null && payment.VNPayStatus == VNPayStatus.Success)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "PaymentRemain này đã được ghi nhận thanh toán trước đó.",
                        Data = false
                    };
                }

                payment.PaidAt = DateTime.Now;
                payment.VNPayStatus = VNPayStatus.Success;

                if (!string.IsNullOrWhiteSpace(gatewayTransactionRef))
                {
                    payment.GatewayTransactionRef = gatewayTransactionRef;
                }

                _unitOfWork.PaymentRemains.Update(payment);
                await _unitOfWork.CommitAsync();

                // Recalculate Invoice + SalesOrder + CustomerDebt
                await RecalculateInvoiceAndOrderAsync(payment.InvoiceId);

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Success = true,
                    Message = "Ghi nhận thanh toán thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi MarkPaymentSuccessAsync({PaymentRemainId})", paymentRemainId);

                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Có lỗi xảy ra khi ghi nhận thanh toán.",
                    Data = false
                };
            }
        }

        private async Task RecalculateInvoiceAndOrderAsync(int invoiceId)
        {
            // không throw, chỉ log nếu lỗi
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.PaymentRemains)
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.PaymentRemains)
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.CustomerDebts)
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.SalesQuotation)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return;

                var order = invoice.SalesOrder;
                if (order == null)
                    return;

                // 1) Tính lại Invoice
                var oldInvoicePaid = invoice.TotalPaid;

                var remainPaid = invoice.PaymentRemains
                    .Where(p => p.PaidAt != null)
                    .Sum(p => p.Amount);

                var totalPaid = invoice.TotalDeposit + remainPaid;

                invoice.TotalPaid = totalPaid;
                invoice.TotalRemain = Math.Max(0, invoice.TotalAmount - totalPaid);

                if (totalPaid <= 0)
                {
                    invoice.PaymentStatus = PaymentStatus.NotPaymentYet;
                }
                else if (totalPaid == invoice.TotalDeposit && totalPaid < invoice.TotalAmount)
                {
                    invoice.PaymentStatus = PaymentStatus.Deposited;
                }
                else if (totalPaid > invoice.TotalDeposit && totalPaid < invoice.TotalAmount)
                {
                    invoice.PaymentStatus = PaymentStatus.PartiallyPaid;
                }
                else if (totalPaid >= invoice.TotalAmount)
                {
                    invoice.PaymentStatus = PaymentStatus.Paid;
                    invoice.TotalRemain = 0;
                }

                // Phần tiền mới tăng thêm cho invoice này
                var deltaPaid = totalPaid - oldInvoicePaid;
                if (deltaPaid < 0)
                {
                    deltaPaid = 0;
                }

                // 2) Tính lại SalesOrder
                order.PaidAmount += deltaPaid;

                decimal depositRequired = 0m;
                if (order.SalesQuotation != null)
                {
                    depositRequired = decimal.Round(
                        order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                        0,
                        MidpointRounding.AwayFromZero);
                }

                if (order.PaidAmount <= 0)
                {
                    order.PaymentStatus = PaymentStatus.NotPaymentYet;
                    order.IsDeposited = false;
                }
                else if (depositRequired > 0)
                {
                    if (order.PaidAmount < depositRequired)
                    {
                        order.PaymentStatus = PaymentStatus.NotPaymentYet; // chưa đủ cọc
                        order.IsDeposited = false;
                    }
                    else if (order.PaidAmount == depositRequired && order.PaidAmount < order.TotalPrice)
                    {
                        order.PaymentStatus = PaymentStatus.Deposited;
                        order.IsDeposited = true;
                    }
                    else if (order.PaidAmount > depositRequired && order.PaidAmount < order.TotalPrice)
                    {
                        order.PaymentStatus = PaymentStatus.PartiallyPaid;
                        order.IsDeposited = true;
                    }
                    else if (order.PaidAmount >= order.TotalPrice)
                    {
                        order.PaymentStatus = PaymentStatus.Paid;
                        order.IsDeposited = true;
                        if (order.PaidFullAt == default)
                            order.PaidFullAt = DateTime.Now;
                    }
                }
                else
                {
                    // Không có % cọc, chỉ check theo tổng
                    if (order.PaidAmount < order.TotalPrice)
                    {
                        order.PaymentStatus = PaymentStatus.PartiallyPaid;
                        order.IsDeposited = false;
                    }
                    else
                    {
                        order.PaymentStatus = PaymentStatus.Paid;
                        order.IsDeposited = true;
                        if (order.PaidFullAt == default)
                            order.PaidFullAt = DateTime.Now;
                    }
                }

                // 3) Cập nhật CustomerDebt (nếu có)
                if (order.CustomerDebts != null)
                {
                    var debt = order.CustomerDebts;
                    debt.DebtAmount = order.TotalPrice - order.PaidAmount;

                    if (debt.DebtAmount <= 0)
                    {
                        debt.status = CustomerDebtStatus.NoDebt;
                    }
                    else
                    {
                        if (DateTime.Now > order.SalesOrderExpiredDate)
                            debt.status = CustomerDebtStatus.OverTime;
                        else
                            debt.status = CustomerDebtStatus.UnPaid;
                    }

                    _unitOfWork.CustomerDebt.Update(debt);
                }

                _unitOfWork.Invoices.Update(invoice);
                _unitOfWork.SalesOrder.Update(order);

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi RecalculateInvoiceAndOrderAsync({InvoiceId})",
                    invoiceId);
            }
        }

        public async Task<ServiceResult<PaymentRemainItemDTO>>CreateBankTransferCheckRequestForInvoiceAsync(int invoiceId, CreateBankTransferCheckRequestDTO request)
        {
            try
            {
                var createResult = await CreatePaymentRemainForInvoiceAsync(
                    new CreatePaymentRemainRequestDTO
                    {
                        InvoiceId = invoiceId,
                        Amount = request.Amount,
                        PaymentMethod = PaymentMethod.BankTransfer,
                        PaymentType = PaymentType.Remain
                    });

                if (!createResult.Success || createResult.Data == null)
                {
                    return new ServiceResult<PaymentRemainItemDTO>
                    {
                        StatusCode = createResult.StatusCode,
                        Success = false,
                        Message = createResult.Message,
                        Data = null
                    };
                }

                var pr = await _unitOfWork.PaymentRemains.Query()
                    .FirstOrDefaultAsync(p => p.Id == createResult.Data.Id);

                if (pr != null)
                {
                    pr.CustomerNote = request.CustomerNote;
                    pr.VNPayStatus = VNPayStatus.Pending;
                    pr.PaymentMethod = PaymentMethod.BankTransfer;
                    pr.Gateway = null;
                    pr.GatewayTransactionRef = null;

                    _unitOfWork.PaymentRemains.Update(pr);
                    await _unitOfWork.CommitAsync();
                }

                return createResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi CreateBankTransferCheckRequestForInvoiceAsync({InvoiceId})", invoiceId);
                return new ServiceResult<PaymentRemainItemDTO>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Có lỗi khi tạo yêu cầu kiểm tra chuyển khoản.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<bool>> ApproveBankTransferRequestAsync(int paymentRemainId)
        {
            var pr = await _unitOfWork.PaymentRemains.Query()
                .FirstOrDefaultAsync(p => p.Id == paymentRemainId);

            if (pr == null)
                return new ServiceResult<bool>
                {
                    StatusCode = 404,
                    Success = false,
                    Message = "Không tìm thấy PaymentRemain.",
                    Data = false
                };

            if (pr.PaymentMethod != PaymentMethod.BankTransfer)
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Success = false,
                    Message = "Yêu cầu này không phải thanh toán chuyển khoản.",
                    Data = false
                };

            if (pr.VNPayStatus == VNPayStatus.Success)
                return new ServiceResult<bool>
                {
                    StatusCode = 400,
                    Success = false,
                    Message = "Yêu cầu này đã được duyệt trước đó.",
                    Data = false
                };

            return await MarkPaymentSuccessAsync(paymentRemainId);
        }

        public async Task<ServiceResult<bool>> RejectBankTransferRequestAsync(int paymentRemainId, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "Vui lòng nhập lý do từ chối.",
                        Data = false
                    };
                }

                var pr = await _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.Invoice)
                    .Include(p => p.SalesOrder)
                    .FirstOrDefaultAsync(p => p.Id == paymentRemainId);

                if (pr == null)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 404,
                        Success = false,
                        Message = "Không tìm thấy PaymentRemain.",
                        Data = false
                    };
                }

                if (pr.PaymentMethod != PaymentMethod.BankTransfer)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "Yêu cầu này không phải thanh toán chuyển khoản.",
                        Data = false
                    };
                }

                if (pr.VNPayStatus == VNPayStatus.Success)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = 400,
                        Success = false,
                        Message = "Yêu cầu này đã được duyệt thanh toán, không thể từ chối.",
                        Data = false
                    };
                }

                pr.VNPayStatus = VNPayStatus.Failed; 
                pr.RejectReason = reason.Trim();
                pr.PaidAt = null;

                _unitOfWork.PaymentRemains.Update(pr);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Success = true,
                    Message = "Đã từ chối yêu cầu xác nhận chuyển khoản.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi RejectBankTransferRequestAsync({PaymentRemainId})", paymentRemainId);
                return new ServiceResult<bool>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Có lỗi khi từ chối yêu cầu.",
                    Data = false
                };
            }
        }

    }
}
