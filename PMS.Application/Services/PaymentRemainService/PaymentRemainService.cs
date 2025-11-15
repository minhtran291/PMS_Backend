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
                    PaidAt = DateTime.Now,
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
    }
}
