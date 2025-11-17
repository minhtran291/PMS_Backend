using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMS.Application.DTOs.VietQR;
using PMS.Application.Services.Base;
using PMS.Core.ConfigOptions;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.DatabaseConfig;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.VietQR
{
    public class VietQrService : IVietQrService
    {
        private readonly PMSContext _db;             
        private readonly VietQRConfig _opt;
        private readonly ILogger<VietQrService> _logger;

        public VietQrService(
            PMSContext db,                       
            IOptions<VietQRConfig> options,
            ILogger<VietQrService> logger)
        {
            _db = db;
            _opt = options.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> ConfirmAsync(VietQrConfirmRequest req)
        {
            // Xác nhận thanh toán (thủ công hoặc webhook đối soát) -> cập nhật trạng thái & công nợ
            try
            {
                var order = await _db.SalesOrders
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == req.SalesOrderId);

                if (order == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.SalesOrderStatus != SalesOrderStatus.Approved && order.PaymentStatus != PaymentStatus.Deposited)
                    return ServiceResult<bool>.Fail("Chỉ xác nhận thanh toán cho đơn hàng được chấp nhận hoặc trạng thái thanh toán là đã đặt cọc.", 400);

                var depositAmount = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0, MidpointRounding.AwayFromZero);

                var remaining = order.TotalPrice - order.PaidAmount;
                if (remaining <= 0)
                    return ServiceResult<bool>.Fail("Đơn đã được thanh toán đủ.", 400);

                var type = req.PaymentType?.Trim().ToLowerInvariant();
                decimal expectedAmount;

                switch (type)
                {
                    case "deposit":
                        // Không cho đặt cọc thêm nếu đã cọc đủ rồi
                        if (order.PaidAmount >= depositAmount)
                            return ServiceResult<bool>.Fail("Đơn đã được đặt cọc trước đó.", 400);
                        expectedAmount = depositAmount;
                        break;

                    case "remain":
                        // Thanh toán phần còn thiếu
                        expectedAmount = remaining;
                        break;

                    case "full":
                        // Nếu chưa thanh toán gì -> full = tổng tiền
                        // Nếu đã thanh toán (vd: đã cọc) -> full = phần còn lại
                        expectedAmount = order.PaidAmount == 0 ? order.TotalPrice : remaining;
                        break;

                    default:
                        return ServiceResult<bool>.Fail("PaymentType không hợp lệ. (deposit / full / remain)", 400);
                }

                // Nếu AmountReceived có gửi lên thì phải khớp expectedAmount
                var payThisTime = req.AmountReceived ?? expectedAmount;
                if (payThisTime != expectedAmount)
                    return ServiceResult<bool>.Fail(
                        $"Số tiền xác nhận không khớp. Yêu cầu: {expectedAmount}, nhận: {payThisTime}.", 400);

                var newPaid = order.PaidAmount + payThisTime;
                if (newPaid > order.TotalPrice)
                    return ServiceResult<bool>.Fail("Thanh toán vượt quá số tiền đơn hàng.", 400);

                await using var tx = await _db.Database.BeginTransactionAsync();

                order.PaidAmount = newPaid;

                if (order.PaidAmount >= order.TotalPrice)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaidAmount = order.TotalPrice;
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.Deposited;
                }
                order.IsDeposited = order.PaymentStatus is PaymentStatus.Deposited or PaymentStatus.Paid;

                // Cập nhật CustomerDebt
                if (order.CustomerDebts == null)
                {
                    order.CustomerDebts = new CustomerDebt
                    {
                        CustomerId = order.CreateBy,
                        SalesOrderId = order.SalesOrderId,
                        DebtAmount = order.TotalPrice - order.PaidAmount,
                        status = DateTime.Now > order.SalesOrderExpiredDate
                            ? CustomerDebtStatus.BadDebt
                            : CustomerDebtStatus.NoDebt
                    };
                    _db.CustomerDebts.Add(order.CustomerDebts);
                }
                else
                {
                    order.CustomerDebts.DebtAmount = order.TotalPrice - order.PaidAmount;
                    order.CustomerDebts.status = DateTime.Now > order.SalesOrderExpiredDate
                        ? CustomerDebtStatus.BadDebt
                        : CustomerDebtStatus.NoDebt;
                }

                _db.SalesOrders.Update(order);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult<bool>.SuccessResult(true, "Xác nhận thanh toán VietQR thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VietQR Confirm error {@Req}", req);
                return ServiceResult<bool>.Fail("Lỗi xác nhận thanh toán.", 500);
            }
        }

        public async Task<ServiceResult<VietQrInitResponse>> InitAsync(VietQrInitRequest req)
        {
            try
            {
                var order = await _db.SalesOrders
                    .Include(o => o.SalesQuotation)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == req.SalesOrderId);

                if (order == null)
                    return ServiceResult<VietQrInitResponse>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.SalesOrderStatus != SalesOrderStatus.Approved && order.PaymentStatus != PaymentStatus.Deposited)
                    return ServiceResult<VietQrInitResponse>.Fail("Chỉ tạo QR cho đơn Approved/Deposited.", 400);

                var depositAmount = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0, MidpointRounding.AwayFromZero);

                var remaining = order.TotalPrice - order.PaidAmount;
                if (remaining <= 0)
                    return ServiceResult<VietQrInitResponse>.Fail("Đơn đã được thanh toán đủ.", 400);

                var type = req.PaymentType?.Trim().ToLowerInvariant();
                decimal amount;
                string tag;

                switch (type)
                {
                    case "deposit":
                        if (order.PaidAmount >= depositAmount)
                            return ServiceResult<VietQrInitResponse>.Fail("Đơn đã được đặt cọc trước đó.", 400);
                        amount = depositAmount;
                        tag = "DEPO";
                        break;

                    case "remain":
                        amount = remaining;
                        tag = "REMAIN";
                        break;

                    case "full":
                        // Nếu chưa thanh toán gì -> full = tổng
                        // Nếu đã thanh toán (ví dụ đã cọc) -> full = phần còn lại
                        amount = order.PaidAmount == 0 ? order.TotalPrice : remaining;
                        tag = "FULL";
                        break;

                    default:
                        return ServiceResult<VietQrInitResponse>.Fail("PaymentType không hợp lệ. (deposit / full / remain)", 400);
                }

                if (amount <= 0)
                    return ServiceResult<VietQrInitResponse>.Fail("Số tiền không hợp lệ.", 400);

                var transferContent = RemoveDiacritics($"SO{order.SalesOrderId}-{tag}").ToUpperInvariant();

                string Encode(string s) => Uri.EscapeDataString(s);
                var baseUrl = (_opt.BaseImageUrl ?? "https://img.vietqr.io/image").Trim().TrimEnd('/');
                var bank = (_opt.BankCode ?? "mbbank").Trim();
                var acct = (_opt.AccountNumber ?? "").Trim();
                var accName = (_opt.AccountName ?? "").Trim();
                var template = string.IsNullOrWhiteSpace(_opt.Template) ? "compact2" : _opt.Template;

                if (string.IsNullOrWhiteSpace(acct) || string.IsNullOrWhiteSpace(accName))
                    return ServiceResult<VietQrInitResponse>.Fail("Chưa cấu hình VietQR: AccountNumber/AccountName.", 500);

                var imgUrl = $"{baseUrl}/{bank}-{acct}-{template}.png" +
                             $"?amount={(long)amount}" +
                             $"&addInfo={Encode(transferContent)}" +
                             $"&accountName={Encode(accName)}";

                var resp = new VietQrInitResponse
                {
                    QrImageUrl = imgUrl,
                    TransferContent = transferContent,
                    Amount = amount,
                    ExpireAt = DateTime.Now.AddHours(24)
                };

                return ServiceResult<VietQrInitResponse>.SuccessResult(resp, "Tạo VietQR thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VietQR Init error {@Req}", req);
                return ServiceResult<VietQrInitResponse>.Fail("Lỗi tạo VietQR.", 500);
            }
        }

        private static string RemoveDiacritics(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
