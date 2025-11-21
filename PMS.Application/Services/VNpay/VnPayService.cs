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
                // CASE REMAIN – dùng PaymentRemain.Id + Amount
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
                // CASE DEPOSIT / FULL – logic cũ theo SalesOrder
                // ========================================================
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == req.SalesOrderId);

                if (order == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.SalesOrderStatus != SalesOrderStatus.Approved && order.PaymentStatus != PaymentStatus.Deposited)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Chỉ khởi tạo thanh toán cho đơn ở trạng thái Approved hoặc Deposited.", 400);

                var depositAmount = decimal.Round(order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m), 0, MidpointRounding.AwayFromZero);
                var remaining = order.TotalPrice - order.PaidAmount;

                if (remaining <= 0)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Đơn đã được thanh toán đủ.", 400);

                decimal amount;
                string info;

                switch (type)
                {
                    case "deposit":
                        amount = depositAmount;
                        info = $"Dat coc {order.SalesOrderCode} ({order.SalesOrderId})";
                        break;

                    case "full":
                        // Nếu chưa thanh toán gì, full = tổng tiền
                        // Nếu đã có thanh toán (ví dụ nhầm trước đó), cho full = phần còn lại
                        amount = order.PaidAmount == 0 ? order.TotalPrice : remaining;
                        info = $"Thanh toan toan bo {order.SalesOrderCode} ({order.SalesOrderId})";
                        break;

                    default:
                        return ServiceResult<VnPayInitResponseDTO>.Fail("PaymentType không hợp lệ. (deposit / full / remain)", 400);
                }

                if (amount <= 0)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Số tiền thanh toán không hợp lệ.", 400);

                var (url, qr, txnRef) = await _gateway.BuildPaymentAsync(order.SalesOrderId, amount, info, req.Locale, clientIp);

                var data = new VnPayInitResponseDTO
                {
                    PaymentUrl = url,
                    QrBase64 = qr,
                    Amount = amount,
                    PaymentType = type ?? string.Empty,
                    TxnRef = txnRef
                };

                return ServiceResult<VnPayInitResponseDTO>.SuccessResult(data, "Khởi tạo VNPay thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Init VNPay error");
                return ServiceResult<VnPayInitResponseDTO>.Fail("Lỗi khởi tạo thanh toán.", 500);
            }
        }

        //Core xác nhận thanh toán + cập nhật đơn & công nợ(theo form bạn gửi)
        private async Task<ServiceResult<bool>> ConfirmAsync(IQueryCollection query, string source)
        {
            try
            {
                // 0) Chữ ký
                if (!_gateway.ValidateSignature(query))
                    return ServiceResult<bool>.Fail("Chữ ký VNPay không hợp lệ.", 400);

                // 1) Các tham số cần kiểm tra
                var rspCode = _gateway.GetQueryValue(query, "vnp_ResponseCode");
                var txnStatus = _gateway.GetQueryValue(query, "vnp_TransactionStatus"); // ✅ thêm
                var amountStr = _gateway.GetQueryValue(query, "vnp_Amount");
                var txnRef = _gateway.GetQueryValue(query, "vnp_TxnRef") ?? "";
                var tmnCode = _gateway.GetQueryValue(query, "vnp_TmnCode");
                var currCode = _gateway.GetQueryValue(query, "vnp_CurrCode");
                var orderInfo = _gateway.GetQueryValue(query, "vnp_OrderInfo")?.ToLowerInvariant();

                // 2) Điều kiện thành công chuẩn
                if (rspCode != "00" || txnStatus != "00")
                    return ServiceResult<bool>.Fail($"Thanh toán bị từ chối ({rspCode}/{txnStatus}).", 400);

                // 3) Ràng buộc tính toàn vẹn cơ bản
                if (string.IsNullOrWhiteSpace(txnRef))
                    return ServiceResult<bool>.Fail("Thiếu mã tham chiếu giao dịch.", 400);
                if (tmnCode == null || !tmnCode.Equals(_opt.TmnCode, StringComparison.OrdinalIgnoreCase))
                    return ServiceResult<bool>.Fail("TmnCode không khớp.", 400);
                if (!string.IsNullOrEmpty(currCode) &&
                    !string.Equals(currCode, "VND", StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<bool>.Fail("Tiền tệ không hợp lệ.", 400);
                }
                if (!long.TryParse(amountStr, out var amountVnp))
                    return ServiceResult<bool>.Fail("Số tiền không hợp lệ.", 400);

                // TxnRef: "SOID-yyyyMMddHHmmssfff"
                var idPart = txnRef.Split('-').FirstOrDefault();
                if (!int.TryParse(idPart, out var salesOrderId))
                    return ServiceResult<bool>.Fail("Mã tham chiếu không hợp lệ.", 400);

                var paidAmount = (decimal)amountVnp / 100m;

                // ======================================================
                // NEW: ƯU TIÊN TÌM PaymentRemain THEO GatewayTransactionRef
                // ======================================================
                var paymentRemain = await _unitOfWork.PaymentRemains.Query()
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

                // 4) Nạp đơn
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (order == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.SalesOrderStatus != SalesOrderStatus.Approved && order.PaymentStatus != PaymentStatus.Deposited)
                    return ServiceResult<bool>.Fail("Chỉ xác nhận thanh toán cho đơn ở trạng thái Approved và trạng thái thanh toán là Deposited.", 400);

                // Đã thanh toán đủ rồi thì bỏ qua (idempotent)
                if (order.PaidAmount >= order.TotalPrice)
                    return ServiceResult<bool>.SuccessResult(true, "Giao dịch đã được ghi nhận trước đó.", 200);

                // 5) Tính số tiền chuẩn theo loại thanh toán
                var depositAmount = decimal.Round(order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m), 0, MidpointRounding.AwayFromZero);
                var remaining = order.TotalPrice - order.PaidAmount;

                if (remaining <= 0)
                    return ServiceResult<bool>.Fail("Đơn đã được thanh toán đủ.", 400);

                var info = orderInfo ?? string.Empty;

                bool isDeposit = info.Contains("dat coc");
                bool isFull = info.Contains("thanh toan toan bo");
                bool isRemain = info.Contains("thanh toan phan con thieu");

                if (!(isDeposit || isFull || isRemain))
                    return ServiceResult<bool>.Fail("Không xác định được loại thanh toán (deposit/full/remain).", 400);

                // Không cho đặt cọc lại nếu đã cọc rồi
                if (isDeposit && order.PaidAmount >= depositAmount)
                    return ServiceResult<bool>.Fail("Đơn đã được đặt cọc trước đó.", 400);

                decimal expectedAmount;

                if (isDeposit)
                {
                    expectedAmount = depositAmount;
                }
                else if (isRemain)
                {
                    expectedAmount = remaining; // thanh toán phần còn lại
                }
                else // isFull
                {
                    // Nếu chưa thanh toán gì: full = toàn bộ
                    // Nếu đã thanh toán (ví dụ đã cọc): full = phần còn lại
                    expectedAmount = order.PaidAmount == 0 ? order.TotalPrice : remaining;
                }

                if (paidAmount != expectedAmount)
                    return ServiceResult<bool>.Fail($"Số tiền thanh toán không khớp. Yêu cầu: {expectedAmount}, VNPay gửi: {paidAmount}.", 400);

                // 6) Cập nhật PaidAmount & Status
                var newPaid = order.PaidAmount + paidAmount;

                if (newPaid > order.TotalPrice)
                    return ServiceResult<bool>.Fail("Thanh toán vượt quá số tiền đơn hàng.", 400);

                order.PaidAmount = newPaid;

                PaymentStatus newStatus;
                if (order.PaidAmount >= order.TotalPrice)
                {
                    newStatus = PaymentStatus.Paid;
                }
                else
                {
                    // Đã có một phần tiền → coi là Deposited
                    newStatus = PaymentStatus.Deposited;
                }

                order.PaymentStatus = newStatus;
                order.IsDeposited = (newStatus == PaymentStatus.Deposited || newStatus == PaymentStatus.Paid);

                // 7) Cập nhật CustomerDebt
                if (order.CustomerDebts == null)
                {
                    order.CustomerDebts = new PMS.Core.Domain.Entities.CustomerDebt
                    {
                        CustomerId = order.CreateBy,
                        SalesOrderId = order.SalesOrderId,
                        DebtAmount = order.TotalPrice - order.PaidAmount,
                        status = DateTime.Now > order.SalesOrderExpiredDate
                            ? CustomerDebtStatus.BadDebt
                            : CustomerDebtStatus.NoDebt
                    };
                    await _unitOfWork.CustomerDebt.AddAsync(order.CustomerDebts);
                }
                else
                {
                    order.CustomerDebts.DebtAmount = order.TotalPrice - order.PaidAmount;
                    order.CustomerDebts.status = DateTime.Now > order.SalesOrderExpiredDate
                        ? CustomerDebtStatus.BadDebt
                        : CustomerDebtStatus.NoDebt;
                }

                _unitOfWork.SalesOrder.Update(order);
                await _unitOfWork.CommitAsync();

                return ServiceResult<bool>.SuccessResult(true, $"Xác nhận thanh toán thành công qua {source}.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay confirm error ({source})", source);
                return ServiceResult<bool>.Fail("Lỗi xử lý kết quả VNPay.", 500);
            }
        }

        /// <summary>
        /// NEW: Khởi tạo VNPay cho PaymentRemain (thanh toán phần còn lại 1 phiếu xuất)
        /// </summary>
        private async Task<ServiceResult<VnPayInitResponseDTO>> InitVnPayForPaymentRemainInternalAsync(
            int paymentRemainId,
            string clientIp,
            string? locale)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.SalesOrder)
                        .ThenInclude(o => o.SalesQuotation)
                    .Include(p => p.SalesOrder.CustomerDebts)
                    .Include(p => p.GoodsIssueNote)
                    .FirstOrDefaultAsync(p => p.Id == paymentRemainId);

                if (payment == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "Không tìm thấy PaymentRemain.", 404);

                if (payment.Status != PaymentStatus.Pending)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "Chỉ khởi tạo VNPay cho PaymentRemain ở trạng thái Pending.", 400);

                if (payment.Amount <= 0)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "Số tiền thanh toán không hợp lệ.", 400);

                var order = payment.SalesOrder;
                if (order == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail(
                        "PaymentRemain không gắn với SalesOrder.", 400);

                // Info + để Confirm nhận biết đây là remain
                var info = $"Thanh toan phan con thieu SO{order.SalesOrderCode} (PR{payment.Id})";

                // TxnRef: dùng PaymentRemain.Id để gắn chặt giao dịch với PaymentRemain
                var (url, qr, txnRef) = await _gateway.BuildPaymentAsync(
                    payment.Id,          // CHANGED: dùng PaymentRemain.Id
                    payment.Amount,
                    info,
                    locale,
                    clientIp);

                // Ghi lại thông tin gateway vào PaymentRemain
                payment.PaymentMethod = PaymentMethod.VnPay;
                payment.Gateway = "VNPay";
                payment.GatewayTransactionRef = txnRef;
                // (Tuỳ bạn) Có thể chỉ set PaidAt khi Success
                payment.CreateRequestAt = DateTime.Now;

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
                    "Lỗi khởi tạo thanh toán cho PaymentRemain.", 500);
            }
        }

        /// <summary>
        /// NEW: Xử lý thành công cho PaymentRemain (remain theo phiếu xuất)
        /// </summary>
        private async Task<ServiceResult<bool>> HandlePaymentRemainSuccessAsync(
            PaymentRemain payment,
            decimal paidAmount,
            string source)
        {
            var order = payment.SalesOrder;

            if (order == null)
                return ServiceResult<bool>.Fail(
                    "PaymentRemain không gắn với đơn hàng.", 400);

            // Idempotent
            if (payment.Status == PaymentStatus.Success)
            {
                return ServiceResult<bool>.SuccessResult(
                    true,
                    "Giao dịch PaymentRemain đã được ghi nhận trước đó.",
                    200);
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return ServiceResult<bool>.Fail(
                    "Trạng thái PaymentRemain không hợp lệ để xác nhận.", 400);
            }

            if (payment.Amount != paidAmount)
            {
                return ServiceResult<bool>.Fail(
                    $"Số tiền thanh toán không khớp. Yêu cầu: {payment.Amount}, VNPay gửi: {paidAmount}.",
                    400);
            }

            var newPaid = order.PaidAmount + paidAmount;
            if (newPaid > order.TotalPrice)
            {
                return ServiceResult<bool>.Fail(
                    "Thanh toán vượt quá số tiền đơn hàng.", 400);
            }

            order.PaidAmount = newPaid;

            if (order.PaidAmount >= order.TotalPrice)
                order.PaymentStatus = PaymentStatus.Paid;
            else
                order.PaymentStatus = PaymentStatus.Deposited;

            order.IsDeposited = (order.PaymentStatus == PaymentStatus.Deposited
                              || order.PaymentStatus == PaymentStatus.Paid);

            // TODO: chỉnh theo enum CustomerDebtStatus thực tế
            if (order.CustomerDebts == null)
            {
                order.CustomerDebts = new PMS.Core.Domain.Entities.CustomerDebt
                {
                    CustomerId = order.CreateBy,
                    SalesOrderId = order.SalesOrderId,
                    DebtAmount = order.TotalPrice - order.PaidAmount,
                    status = DateTime.Now > order.SalesOrderExpiredDate
                        ? CustomerDebtStatus.BadDebt
                        : CustomerDebtStatus.NoDebt
                };
                await _unitOfWork.CustomerDebt.AddAsync(order.CustomerDebts);
            }
            else
            {
                order.CustomerDebts.DebtAmount = order.TotalPrice - order.PaidAmount;
                order.CustomerDebts.status = DateTime.Now > order.SalesOrderExpiredDate
                    ? CustomerDebtStatus.BadDebt
                    : CustomerDebtStatus.NoDebt;
            }

            // Cập nhật PaymentRemain
            payment.Status = PaymentStatus.Success;
            payment.PaymentMethod = PaymentMethod.VnPay;
            payment.Gateway = "VNPay";
            payment.CreateRequestAt = DateTime.Now;

            _unitOfWork.PaymentRemains.Update(payment);
            _unitOfWork.SalesOrder.Update(order);
            await _unitOfWork.CommitAsync();

            return ServiceResult<bool>.SuccessResult(
                true,
                $"Xác nhận thanh toán phần còn lại thành công qua {source}.",
                200);
        }

    }
}