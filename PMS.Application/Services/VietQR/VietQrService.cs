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

                if (order.Status != SalesOrderStatus.Approved && order.Status != SalesOrderStatus.Deposited)
                    return ServiceResult<bool>.Fail("Chỉ xác nhận thanh toán cho đơn Approved/Deposited.", 400);

                var depositAmount = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0, MidpointRounding.AwayFromZero);

                var payThisTime = req.AmountReceived ??
                                  ((req.PaymentType?.ToLowerInvariant() == "full") ? order.TotalPrice : depositAmount);

                if (payThisTime <= 0)
                    return ServiceResult<bool>.Fail("Số tiền xác nhận không hợp lệ.", 400);

                await using var tx = await _db.Database.BeginTransactionAsync();

                var accumulated = Math.Min(order.PaidAmount + payThisTime, order.TotalPrice);
                if (accumulated >= order.TotalPrice)
                {
                    order.Status = SalesOrderStatus.Paid;
                    order.PaidAmount = order.TotalPrice;
                }
                else
                {
                    order.Status = SalesOrderStatus.Deposited;
                    order.PaidAmount = accumulated;
                }
                order.IsDeposited = order.Status is SalesOrderStatus.Deposited or SalesOrderStatus.Paid;

                if (order.CustomerDebts == null)
                {
                    order.CustomerDebts = new CustomerDebt
                    {
                        CustomerId = order.CreateBy, // entity của bạn: string
                        SalesOrderId = order.SalesOrderId,
                        DebtAmount = order.TotalPrice - order.PaidAmount,
                        status = DateTime.Now > order.SalesOrderExpiredDate
                            ? CustomerDebtStatus.OverTime
                            : CustomerDebtStatus.OnTime
                    };
                    _db.CustomerDebts.Add(order.CustomerDebts);
                }
                else
                {
                    order.CustomerDebts.DebtAmount = order.TotalPrice - order.PaidAmount;
                    order.CustomerDebts.status = DateTime.Now > order.SalesOrderExpiredDate
                        ? CustomerDebtStatus.OverTime
                        : CustomerDebtStatus.OnTime;
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

                if (order.Status != SalesOrderStatus.Approved && order.Status != SalesOrderStatus.Deposited)
                    return ServiceResult<VietQrInitResponse>.Fail("Chỉ tạo QR cho đơn Approved/Deposited.", 400);

                var deposit = decimal.Round(
                    order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m),
                    0, MidpointRounding.AwayFromZero);

                var amount = (req.PaymentType?.ToLowerInvariant() == "full") ? order.TotalPrice : deposit;
                if (amount <= 0)
                    return ServiceResult<VietQrInitResponse>.Fail("Số tiền không hợp lệ.", 400);

                var tag = (req.PaymentType?.ToLowerInvariant() == "full") ? "FULL" : "DEPO";
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
