using MailKit.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMS.API.Helpers.VnPay;
using PMS.Application.DTOs.VnPay;
using PMS.Application.DTOs.VNPay;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.VNpay
{
    public class VnPayService : IVnPayService
    {
        private readonly IVnPayGateway _gateway;
        private readonly VNPayConfig _opt;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VnPayService> _logger;

        public VnPayService(
            IVnPayGateway gateway, 
            IUnitOfWork uow,
            IOptions<VNPayConfig> opt,
            ILogger<VnPayService> logger)
        {
            _gateway = gateway;
            _unitOfWork = uow;
            _opt = opt.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> HandleVnPayIpnAsync(IQueryCollection query)
        {
            return await ConfirmAsync(query, "ipn");
        }

        public async Task<ServiceResult<bool>> HandleVnPayReturnAsync(IQueryCollection query)
        {
            return await ConfirmAsync(query, "return");
        }

        /// <summary>
        /// INIT VNPay:
        /// - deposit/full: theo SalesOrder
        /// - remain: theo PaymentRemain
        /// </summary>
        public async Task<ServiceResult<VnPayInitResponseDTO>> InitVnPayAsync(VnPayInitRequestDTO req, string clientIp)
        {
            try
            {
                var type = req.PaymentType?.Trim().ToLowerInvariant();

                // ========================================================
                // 1) remain  -> thanh toán theo PaymentRemain (Invoice)
                // ========================================================
                if (type == "remain")
                {
                    if (!req.PaymentRemainId.HasValue)
                    {
                        return ServiceResult<VnPayInitResponseDTO>.Fail(
                            "Thiếu PaymentRemainId cho kiểu thanh toán 'remain'.", 400);
                    }

                    return await InitVnPayForPaymentRemainInternalAsync(
                        req.PaymentRemainId.Value,
                        clientIp,
                        req.Locale);
                }

                // ========================================================
                // 2) deposit / full  -> thanh toán theo SalesOrder
                // ========================================================
                if (type == "deposit" || type == "full")
                {
                    if (req.SalesOrderId == null || req.SalesOrderId <= 0)
                    {
                        return ServiceResult<VnPayInitResponseDTO>.Fail(
                            "Thiếu SalesOrderId cho kiểu thanh toán 'deposit' hoặc 'full'.", 400);
                    }
                }
                else
                {
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "PaymentType không hợp lệ. (deposit / full / remain)", 400);
                }

                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == req.SalesOrderId);

                if (order == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.SalesOrderStatus != SalesOrderStatus.Approved
                    && order.PaymentStatus != PaymentStatus.Deposited
                    && order.PaymentStatus != PaymentStatus.PartiallyPaid)
                {
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "Chỉ khởi tạo thanh toán cho đơn ở trạng thái Approved hoặc đã có thanh toán trước đó.",
                        400);
                }

                if (order.SalesQuotation == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "Đơn hàng chưa gắn với báo giá, không xác định được % cọc.",
                        400);

                var depositRequired = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                var remaining = order.TotalPrice - order.PaidAmount;
                if (remaining <= 0)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Đơn đã được thanh toán đủ.", 400);

                decimal amount;
                string orderInfo;

                switch (type)
                {
                    case "deposit":
                        amount = depositRequired;
                        orderInfo = $"Dat coc {order.SalesOrderCode} ({order.SalesOrderId})";
                        break;

                    case "full":
                        amount = order.PaidAmount == 0 ? order.TotalPrice : remaining;
                        orderInfo = $"Thanh toan toan bo {order.SalesOrderCode} ({order.SalesOrderId})";
                        break;

                    default:
                        return ServiceResult<VnPayInitResponseDTO>.Fail(
                            "PaymentType không hợp lệ. (deposit / full / remain)", 400);
                }

                if (amount <= 0)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "Số tiền thanh toán không hợp lệ.", 400);

                // TxnRef kiểu cũ: "SOID-yyyyMMddHHmmssfff"
                var (url, qr, txnRef) = await _gateway.BuildPaymentAsync(
                    order.SalesOrderId,
                    amount,
                    orderInfo,
                    req.Locale,
                    clientIp);

                var data = new VnPayInitResponseDTO
                {
                    PaymentUrl = url,
                    QrBase64 = qr,
                    Amount = amount,
                    PaymentType = type ?? string.Empty,
                    TxnRef = txnRef
                };

                return ServiceResult<VnPayInitResponseDTO>.SuccessResult(
                    data,
                    "Khởi tạo VNPay thành công.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Init VNPay error");
                return ServiceResult<VnPayInitResponseDTO>.Fail(
                    "Lỗi khởi tạo thanh toán.",
                    500);
            }
        }

        //Core xác nhận thanh toán + cập nhật đơn & công nợ(theo form bạn gửi)
        private async Task<ServiceResult<bool>> ConfirmAsync(IQueryCollection query, string source)
        {
            try
            {
                // 0) Verify chữ ký
                if (!_gateway.ValidateSignature(query))
                    return ServiceResult<bool>.Fail("Chữ ký VNPay không hợp lệ.", 400);

                // 1) Đọc tham số
                var rspCode = _gateway.GetQueryValue(query, "vnp_ResponseCode");
                var txnStatus = _gateway.GetQueryValue(query, "vnp_TransactionStatus");
                var amountStr = _gateway.GetQueryValue(query, "vnp_Amount");
                var txnRef = _gateway.GetQueryValue(query, "vnp_TxnRef") ?? "";
                var tmnCode = _gateway.GetQueryValue(query, "vnp_TmnCode");
                var currCode = _gateway.GetQueryValue(query, "vnp_CurrCode");
                var orderInfo = _gateway.GetQueryValue(query, "vnp_OrderInfo")?.ToLowerInvariant() ?? "";

                // 2) Điều kiện thành công
                if (rspCode != "00" || txnStatus != "00")
                    return ServiceResult<bool>.Fail(
                        $"Thanh toán bị từ chối ({rspCode}/{txnStatus}).",
                        400);

                if (string.IsNullOrWhiteSpace(txnRef))
                    return ServiceResult<bool>.Fail("Thiếu mã tham chiếu giao dịch.", 400);

                if (tmnCode == null ||
                    !tmnCode.Equals(_opt.TmnCode, StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<bool>.Fail("TmnCode không khớp.", 400);
                }

                if (!string.IsNullOrEmpty(currCode) &&
                    !string.Equals(currCode, "VND", StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<bool>.Fail("Tiền tệ không hợp lệ.", 400);
                }

                if (!long.TryParse(amountStr, out var amountVnp))
                    return ServiceResult<bool>.Fail("Số tiền không hợp lệ.", 400);

                var paidAmount = (decimal)amountVnp / 100m;

                // ======================================================
                // 1)luồng PaymentRemain (Invoice)
                //    Tìm bằng GatewayTransactionRef
                // ======================================================
                var paymentRemain = await _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.Invoice)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(o => o.SalesQuotation)
                    .Include(p => p.SalesOrder.CustomerDebts)
                    .FirstOrDefaultAsync(p => p.GatewayTransactionRef == txnRef);

                if (paymentRemain != null)
                {
                    return await HandlePaymentRemainSuccessAsync(
                        paymentRemain,
                        paidAmount,
                        source);
                }

                // ======================================================
                // 2) Fallback: luồng SalesOrder (deposit / full)
                //    TxnRef dạng "SOID-yyyyMMddHHmmssfff"
                // ======================================================
                var idPart = txnRef.Split('-').FirstOrDefault();
                if (!int.TryParse(idPart, out var salesOrderId))
                    return ServiceResult<bool>.Fail("Mã tham chiếu không hợp lệ.", 400);

                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (order == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.SalesQuotation == null)
                    return ServiceResult<bool>.Fail(
                        "Đơn hàng chưa gắn với báo giá, không xác định được % cọc.",
                        400);

                var depositRequired = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                var remaining = order.TotalPrice - order.PaidAmount;
                if (remaining <= 0)
                    return ServiceResult<bool>.Fail("Đơn đã được thanh toán đủ.", 400);

                bool isDeposit = orderInfo.Contains("dat coc");
                bool isFull = orderInfo.Contains("thanh toan toan bo");

                if (!(isDeposit || isFull))
                    return ServiceResult<bool>.Fail(
                        "Không xác định được loại thanh toán (deposit / full).",
                        400);

                // Không cho đặt cọc lại nếu đã cọc đủ
                if (isDeposit && order.PaidAmount >= depositRequired)
                    return ServiceResult<bool>.Fail(
                        "Đơn đã được đặt cọc trước đó.",
                        400);

                decimal expectedAmount;

                if (isDeposit)
                {
                    expectedAmount = depositRequired;
                }
                else // full
                {
                    expectedAmount = order.PaidAmount == 0 ? order.TotalPrice : remaining;
                }

                if (paidAmount != expectedAmount)
                    return ServiceResult<bool>.Fail(
                        $"Số tiền thanh toán không khớp. Yêu cầu: {expectedAmount}, VNPay gửi: {paidAmount}.",
                        400);

                // -----------------------------
                // Cập nhật SalesOrder & CustomerDebt
                // -----------------------------
                order.PaidAmount += paidAmount;

                PaymentStatus newStatus;
                if (order.PaidAmount <= 0)
                    newStatus = PaymentStatus.NotPaymentYet;
                else if (order.PaidAmount < depositRequired)
                    newStatus = PaymentStatus.PartiallyPaid;
                else if (order.PaidAmount < order.TotalPrice)
                    newStatus = PaymentStatus.Deposited;
                else
                    newStatus = PaymentStatus.Paid;

                order.PaymentStatus = newStatus;
                order.IsDeposited = (newStatus == PaymentStatus.Deposited || newStatus == PaymentStatus.Paid);

                if (newStatus == PaymentStatus.Paid && order.PaidFullAt == default)
                    order.PaidFullAt = DateTime.Now;

                var remainDebt = order.TotalPrice - order.PaidAmount;

                if (order.CustomerDebts == null)
                {
                    order.CustomerDebts = new PMS.Core.Domain.Entities.CustomerDebt
                    {
                        CustomerId = order.CreateBy,
                        SalesOrderId = order.SalesOrderId,
                        DebtAmount = remainDebt,
                        // TODO: cập nhật status theo enum CustomerDebtStatus thật của bạn
                    };
                    await _unitOfWork.CustomerDebt.AddAsync(order.CustomerDebts);
                }
                else
                {
                    order.CustomerDebts.DebtAmount = remainDebt;
                }

                _unitOfWork.SalesOrder.Update(order);
                _unitOfWork.CustomerDebt.Update(order.CustomerDebts);
                await _unitOfWork.CommitAsync();

                return ServiceResult<bool>.SuccessResult(
                    true,
                    $"Xác nhận thanh toán {(isDeposit ? "cọc" : "full")} thành công qua {source}.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay confirm error ({source})", source);
                return ServiceResult<bool>.Fail(
                    "Lỗi xử lý kết quả VNPay.",
                    500);
            }
        }

        /// <summary>
        /// Khởi tạo VNPay cho PaymentRemain (thanh toán phần còn lại 1 phiếu xuất)
        /// </summary>
        private async Task<ServiceResult<VnPayInitResponseDTO>> InitVnPayForPaymentRemainInternalAsync(
            int paymentRemainId,
            string clientIp,
            string? locale)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.Invoice)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(p => p.Id == paymentRemainId);

                if (payment == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Không tìm thấy PaymentRemain.", 404);

                if (payment.VNPayStatus == VNPayStatus.Success)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "PaymentRemain này đã được thanh toán trước đó.", 400);

                if (payment.Amount <= 0)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "Số tiền thanh toán không hợp lệ.", 400);

                var invoice = payment.Invoice;
                if (invoice == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "PaymentRemain không gắn với Invoice.", 400);

                var info = $"Thanh toan hoa don {invoice.InvoiceCode} (PR{payment.Id})";

                // TxnRef: dùng PaymentRemain.Id để confirm
                var (url, qr, txnRef) = await _gateway.BuildPaymentAsync(
                    payment.Id,
                    payment.Amount,
                    info,
                    locale,
                    clientIp);

                payment.PaymentMethod = PaymentMethod.VnPay;
                payment.Gateway = "VNPay";
                payment.GatewayTransactionRef = txnRef;
                payment.CreateRequestAt = DateTime.Now;
                payment.VNPayStatus = VNPayStatus.Pending;

                _unitOfWork.PaymentRemains.Update(payment);
                await _unitOfWork.CommitAsync();

                var data = new VnPayInitResponseDTO
                {
                    PaymentUrl = url,
                    QrBase64 = qr,
                    Amount = payment.Amount,
                    PaymentType = "remain",
                    TxnRef = txnRef
                };

                return ServiceResult<VnPayInitResponseDTO>.SuccessResult(
                    data,
                    "Khởi tạo VNPay cho PaymentRemain thành công.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Init VNPay for PaymentRemain error ({PaymentRemainId})",
                    paymentRemainId);

                return ServiceResult<VnPayInitResponseDTO>.Fail(
                    "Lỗi khởi tạo thanh toán cho PaymentRemain.",
                    500);
            }
        }

        /// <summary>
        /// Xử lý thành công cho PaymentRemain (remain theo phiếu xuất)
        /// </summary>
        private async Task<ServiceResult<bool>> HandlePaymentRemainSuccessAsync(
            PaymentRemain payment,
            decimal paidAmount,
            string source)
        {
            var order = payment.SalesOrder;
            var invoice = payment.Invoice;

            if (order == null || invoice == null)
                return ServiceResult<bool>.Fail(
                    "PaymentRemain không gắn đủ SalesOrder/Invoice.", 400);

            // Idempotent
            if (payment.VNPayStatus == VNPayStatus.Success)
            {
                return ServiceResult<bool>.SuccessResult(
                    true,
                    "Giao dịch PaymentRemain đã được ghi nhận trước đó.",
                    200);
            }

            if (payment.VNPayStatus != VNPayStatus.Pending)
            {
                return ServiceResult<bool>.Fail(
                    "Trạng thái PaymentRemain không hợp lệ để xác nhận.",
                    400);
            }

            if (payment.Amount != paidAmount)
            {
                return ServiceResult<bool>.Fail(
                    $"Số tiền thanh toán không khớp. Yêu cầu: {payment.Amount}, VNPay gửi: {paidAmount}.",
                    400);
            }

            // Cập nhật Invoice
            invoice.TotalPaid += paidAmount;
            invoice.TotalRemain = invoice.TotalAmount - invoice.TotalPaid;

            if (invoice.TotalPaid <= 0)
                invoice.PaymentStatus = PaymentStatus.NotPaymentYet;
            else if (invoice.TotalPaid < invoice.TotalAmount)
                invoice.PaymentStatus = PaymentStatus.PartiallyPaid;
            else
                invoice.PaymentStatus = PaymentStatus.Paid;

            // Cập nhật SalesOrder 
            order.PaidAmount += paidAmount;

            // Tính tiền cọc yêu cầu
            decimal depositRequired = 0m;
            if (order.SalesQuotation != null)
            {
                depositRequired = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);
            }

            if (order.PaidAmount <= 0)
                order.PaymentStatus = PaymentStatus.NotPaymentYet;
            else if (order.PaidAmount < depositRequired)
                order.PaymentStatus = PaymentStatus.PartiallyPaid;
            else if (order.PaidAmount < order.TotalPrice)
                order.PaymentStatus = PaymentStatus.Deposited;
            else
                order.PaymentStatus = PaymentStatus.Paid;

            if (order.PaymentStatus == PaymentStatus.Paid && order.PaidFullAt == default)
                order.PaidFullAt = DateTime.Now;

            order.IsDeposited = order.PaymentStatus == PaymentStatus.Deposited
                             || order.PaymentStatus == PaymentStatus.Paid;

            // Cập nhật CustomerDebt
            var remainDebt = order.TotalPrice - order.PaidAmount;
            var debt = order.CustomerDebts;

            if (debt == null)
            {
                debt = new PMS.Core.Domain.Entities.CustomerDebt
                {
                    CustomerId = order.CreateBy,
                    SalesOrderId = order.SalesOrderId,
                    DebtAmount = remainDebt,
                };
                await _unitOfWork.CustomerDebt.AddAsync(debt);
                order.CustomerDebts = debt;
            }
            else
            {
                debt.DebtAmount = remainDebt;
            }

            payment.VNPayStatus = VNPayStatus.Success;
            payment.PaymentMethod = PaymentMethod.VnPay;
            payment.Gateway = payment.Gateway ?? "VNPay";
            payment.PaidAt = DateTime.Now;

            _unitOfWork.PaymentRemains.Update(payment);
            _unitOfWork.Invoices.Update(invoice);
            _unitOfWork.SalesOrder.Update(order);
            _unitOfWork.CustomerDebt.Update(debt);

            await _unitOfWork.CommitAsync();

            return ServiceResult<bool>.SuccessResult(
                true,
                $"Xác nhận thanh toán PaymentRemain thành công qua {source}.",
                200);
        }

    }
}