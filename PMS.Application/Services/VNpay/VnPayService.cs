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

        public async Task<ServiceResult<VnPayInitResponseDTO>> InitVnPayAsync(VnPayInitRequestDTO req, string clientIp)
        {
            try
            {
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == req.SalesOrderId);

                if (order == null)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.Status != SalesOrderStatus.Approved && order.Status != SalesOrderStatus.Deposited)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Chỉ khởi tạo thanh toán cho đơn ở trạng thái Approved hoặc Deposited.", 400);

                var depositAmount = decimal.Round(order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m), 0, MidpointRounding.AwayFromZero);
                var amount = (req.PaymentType?.ToLowerInvariant() == "full") ? order.TotalPrice : depositAmount;

                if (amount <= 0)
                    return ServiceResult<VnPayInitResponseDTO>.Fail("Số tiền thanh toán không hợp lệ.", 400);

                var info = req.PaymentType?.ToLowerInvariant() == "full"
                    ? $"Thanh toan toan bo SO#{order.SalesOrderCode} ({order.SalesOrderId})"
                    : $"Dat coc SO#{order.SalesOrderCode} ({order.SalesOrderId})";

                var (url, qr, txnRef) = await _gateway.BuildPaymentAsync(order.SalesOrderId, amount, info, req.BankCode, req.Locale, clientIp);

                var data = new VnPayInitResponseDTO { PaymentUrl = url, QrBase64 = qr, Amount = amount, TxnRef = txnRef };
                return ServiceResult<VnPayInitResponseDTO>.SuccessResult(data, "Khởi tạo VNPay thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Init VNPay error");
                return ServiceResult<VnPayInitResponseDTO>.Fail("Lỗi khởi tạo thanh toán.", 500);
            }
        }

        // Core xác nhận thanh toán + cập nhật đơn & công nợ (theo form bạn gửi)
        private async Task<ServiceResult<bool>> ConfirmAsync(IQueryCollection query, string source)
        {
            try
            {
                if (!_gateway.ValidateSignature(query))
                    return ServiceResult<bool>.Fail("Chữ ký VNPay không hợp lệ.", 400);

                var rspCode = _gateway.GetQueryValue(query, "vnp_ResponseCode");
                var amountStr = _gateway.GetQueryValue(query, "vnp_Amount");
                var txnRef = _gateway.GetQueryValue(query, "vnp_TxnRef") ?? "";

                if (rspCode != "00")
                    return ServiceResult<bool>.Fail($"Thanh toán bị từ chối ({rspCode}).", 400);

                if (string.IsNullOrWhiteSpace(txnRef))
                    return ServiceResult<bool>.Fail("Thiếu mã tham chiếu giao dịch.", 400);

                var idPart = txnRef.Split('-').FirstOrDefault();
                if (!int.TryParse(idPart, out var salesOrderId))
                    return ServiceResult<bool>.Fail("Mã tham chiếu không hợp lệ.", 400);

                if (!long.TryParse(amountStr, out var amountVnp))
                    return ServiceResult<bool>.Fail("Số tiền không hợp lệ.", 400);

                var paidAmount = (decimal)amountVnp / 100m;

                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.CustomerDebts)
                    .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId);

                if (order == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy đơn hàng.", 404);

                if (order.Status != SalesOrderStatus.Approved && order.Status != SalesOrderStatus.Deposited)
                    return ServiceResult<bool>.Fail("Chỉ xác nhận thanh toán cho đơn ở trạng thái Approved hoặc Deposited.", 400);

                var depositAmount = decimal.Round(order.TotalPrice * (order.SalesQuotation.DepositPercent / 100m), 0, MidpointRounding.AwayFromZero);

                // Quy tắc cập nhật giống form bạn đưa:
                SalesOrderStatus newStatus;
                decimal newPaid;

                if (paidAmount >= order.TotalPrice)
                {
                    // trả full trong 1 lần
                    newStatus = SalesOrderStatus.Paid;
                    newPaid = order.TotalPrice;
                }
                else if (paidAmount == depositAmount || order.Status == SalesOrderStatus.Deposited)
                {
                    // cọc hoặc đã cọc trước đó và trả tiếp
                    var accumulated = Math.Min(order.PaidAmount + paidAmount, order.TotalPrice);
                    newPaid = accumulated;
                    newStatus = accumulated >= order.TotalPrice ? SalesOrderStatus.Paid : SalesOrderStatus.Deposited;
                }
                else
                {
                    // các trường hợp lệch số tiền: cộng dồn nhưng không vượt tổng
                    newPaid = Math.Min(order.PaidAmount + paidAmount, order.TotalPrice);
                    newStatus = newPaid >= order.TotalPrice ? SalesOrderStatus.Paid : SalesOrderStatus.Deposited;
                }

                order.PaidAmount = newPaid;
                order.IsDeposited = newStatus == SalesOrderStatus.Deposited || newStatus == SalesOrderStatus.Paid;
                order.Status = newStatus;

                if (order.CustomerDebts == null)
                {
                    order.CustomerDebts = new CustomerDebt
                    {
                        CustomerId = order.CreateBy, // entity của bạn: string
                        SalesOrderId = order.SalesOrderId,
                        DebtAmount = order.TotalPrice - order.PaidAmount,
                        status = DateTime.Now > order.SalesOrderExpiredDate ? CustomerDebtStatus.OverTime : CustomerDebtStatus.OnTime
                    };
                    await _unitOfWork.CustomerDebt.AddAsync(order.CustomerDebts);
                }
                else
                {
                    order.CustomerDebts.DebtAmount = order.TotalPrice - order.PaidAmount;
                    order.CustomerDebts.status = DateTime.Now > order.SalesOrderExpiredDate ? CustomerDebtStatus.OverTime : CustomerDebtStatus.OnTime;
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
    }
}