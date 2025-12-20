using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Invoice;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Application.Services.SmartCA;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.Migrations;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.Invoice
{
    public class InvoiceService(IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger<InvoiceService> logger,
        IPdfService pdfService,
        IEmailService emailService,
        INotificationService notificationService,
        ISmartCAService smartCAService) : Service(unitOfWork, mapper), IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<InvoiceService> _logger = logger;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IEmailService _emailService = emailService;
        private readonly INotificationService _noti = notificationService;
        private readonly ISmartCAService _smartCAService = smartCAService;

        public async Task<ServiceResult<InvoiceDTO>> GenerateInvoiceFromGINAsync(GenerateInvoiceFromGINRequestDTO request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.SalesOrderCode) ||
                    request.GoodsIssueNoteCodes == null ||
                    request.GoodsIssueNoteCodes.Count == 0)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "Mã đơn hàng hoặc danh sách mã phiếu xuất không hợp lệ.",
                        Data = null
                    };
                }

                // 1. SalesOrder theo Code
                var order = await _unitOfWork.SalesOrder.Query()
                    .Include(o => o.SalesQuotation)
                    .Include(o => o.SalesOrderDetails)
                    .FirstOrDefaultAsync(o => o.SalesOrderCode == request.SalesOrderCode);

                if (order == null)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy đơn hàng.",
                        Data = null
                    };
                }

                if (order.SalesQuotation == null)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "đơn hàng chưa gắn với báo giá, không xác định được % cọc.",
                        Data = null
                    };
                }

                // 2. Lấy GoodsIssueNote theo Code
                var goodsIssueNotes = await _unitOfWork.GoodsIssueNote.Query()
                    .Include(n => n.StockExportOrder)
                    .Include(n => n.GoodsIssueNoteDetails)
                    .Where(n => request.GoodsIssueNoteCodes.Contains(n.GoodsIssueNoteCode))
                    .ToListAsync();

                if (!goodsIssueNotes.Any())
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy phiếu xuất tương ứng.",
                        Data = null
                    };
                }

                if (goodsIssueNotes.Any(n => n.StockExportOrder.SalesOrderId != order.SalesOrderId))
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "Các phiếu xuất không thuộc cùng một đơn hàng.",
                        Data = null
                    };
                }

                var orderTotal = order.TotalPrice;

                // 3. Tổng cọc theo % trong SalesQuotation
                var depositFixed = decimal.Round(
                    orderTotal * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                // 4. Tính giá trị từng phiếu xuất
                //Collect tất cả LotId cần tra cứu(lot trong SO + lot trong GIN)
                var soLotIds = order.SalesOrderDetails
                    .Select(x => x.LotId)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var ginLotIds = goodsIssueNotes
                    .SelectMany(n => n.GoodsIssueNoteDetails)
                    .Select(d => d.LotId)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var allLotIds = soLotIds.Concat(ginLotIds).Distinct().ToList();

                var lotInfos = await _unitOfWork.LotProduct.Query()
                    .Where(lp => allLotIds.Contains(lp.LotID))
                    .Select(lp => new
                    {
                        lp.LotID,
                        lp.ProductID,
                        lp.ExpiredDate,   
                        lp.SupplierID
                    })
                    .ToListAsync();

                var lotKeyByLotId = lotInfos
                .GroupBy(x => x.LotID)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    (
                        ProductId: g.First().ProductID,
                        ExpiredDate: g.First().ExpiredDate.Date,
                        SupplierID: g.First().SupplierID
                    )
                );

                var priceKeyToUnitPrice = new Dictionary<(int ProductId, DateTime ExpiredDate, int SupplierID, decimal UnitPrice), decimal>();

                foreach (var so in order.SalesOrderDetails)
                {
                    if (so.LotId <= 0) continue;

                    if (!lotKeyByLotId.TryGetValue(so.LotId, out var soLotKey))
                    {
                        return new ServiceResult<InvoiceDTO>
                        {
                            StatusCode = 400,
                            Message = $"Không lấy được thông tin lô hàng từ SalesOrderDetails (LotId={so.LotId}).",
                            Data = null
                        };
                    }

                    var key = (soLotKey.ProductId, soLotKey.ExpiredDate, soLotKey.SupplierID, so.UnitPrice);

                    if (!priceKeyToUnitPrice.ContainsKey(key))
                        priceKeyToUnitPrice[key] = so.UnitPrice;
                }

                var noteInfos = new List<(PMS.Core.Domain.Entities.GoodsIssueNote Note, decimal NoteAmount)>();

                foreach (var note in goodsIssueNotes)
                {
                    decimal noteAmount = 0m;

                    foreach (var d in note.GoodsIssueNoteDetails)
                    {
                        if (d.LotId <= 0)
                        {
                            return new ServiceResult<InvoiceDTO>
                            {
                                StatusCode = 400,
                                Message = $"Phiếu xuất {note.GoodsIssueNoteCode} có dòng xuất thiếu mã định danh lô hàng.",
                                Data = null
                            };
                        }

                        if (!lotKeyByLotId.TryGetValue(d.LotId, out var ginLotKey))
                        {
                            return new ServiceResult<InvoiceDTO>
                            {
                                StatusCode = 400,
                                Message = $"Không lấy được thông tin lô hàng từ chi tiết phiếu xuất (LotId={d.LotId}).",
                                Data = null
                            };
                        }

                        var candidates = priceKeyToUnitPrice.Keys
                            .Where(k =>
                                k.ProductId == ginLotKey.ProductId &&
                                k.ExpiredDate == ginLotKey.ExpiredDate &&
                                k.SupplierID == ginLotKey.SupplierID)
                            .Select(k => k.UnitPrice)
                            .Distinct()
                            .ToList();

                        if (candidates.Count == 0)
                        {
                            // Đây chính là trường hợp lot thay thế KHÔNG có dòng đặt tương ứng trong SalesOrderDetails
                            return new ServiceResult<InvoiceDTO>
                            {
                                StatusCode = 400,
                                Message =
                                    $"LotId={d.LotId} (ProductId={ginLotKey.ProductId}, HSD={ginLotKey.ExpiredDate:dd-MM-yyyy}, NSX={ginLotKey.SupplierID}) " +
                                    $"không có dòng tương ứng trong chi tiết đơn hàng để lấy giá bán.",
                                Data = null
                            };
                        }

                        if (candidates.Count > 1)
                        {
                            return new ServiceResult<InvoiceDTO>
                            {
                                StatusCode = 400,
                                Message =
                                    $"Dữ liệu chi tiết đơn hàng có nhiều đơn giá cho cùng ProductId={ginLotKey.ProductId}, " +
                                    $"HSD={ginLotKey.ExpiredDate:dd-MM-yyyy}, NSX={ginLotKey.SupplierID}. Không xác định được giá để tính hóa đơn.",
                                Data = null
                            };
                        }

                        var unitPrice = candidates[0];
                        noteAmount += unitPrice * d.Quantity;
                    }

                    noteInfos.Add((note, noteAmount));
                }

                // Sort theo ngày giao + Id
                noteInfos = noteInfos
                    .OrderBy(x => x.Note.DeliveryDate)
                    .ThenBy(x => x.Note.Id)
                    .ToList();

                await _unitOfWork.BeginTransactionAsync();

                var now = DateTime.Now;
                var invoiceCode = BuildInvoiceCode(request);

                // 5. Tạo Invoice
                var invoice = new Core.Domain.Entities.Invoice
                {
                    SalesOrderId = order.SalesOrderId,
                    InvoiceCode = invoiceCode,
                    CreatedAt = now,
                    IssuedAt = now,
                    Status = InvoiceStatus.Draft,
                    TotalAmount = 0m,
                    TotalPaid = 0m,
                    TotalDeposit = 0m,
                    TotalRemain = 0m,
                    PaymentStatus = PaymentStatus.NotPaymentYet
                };

                await _unitOfWork.Invoices.AddAsync(invoice);
                await _unitOfWork.CommitAsync();

                // 6. Tạo InvoiceDetails + tính tiền
                var invoiceDetails = new List<InvoiceDetail>();
                int exportIndex = 1;

                foreach (var (note, noteAmount) in noteInfos)
                {
                    var proportion = noteAmount / orderTotal;

                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);

                    var paidRemain = 0m; // chưa thanh toán phần còn lại
                    var totalPaidForNote = allocatedDeposit + paidRemain;
                    var noteBalance = noteAmount - totalPaidForNote;

                    var detail = new InvoiceDetail
                    {
                        InvoiceId = invoice.Id,
                        GoodsIssueNoteId = note.Id,
                        GoodsIssueAmount = noteAmount,
                        AllocatedDeposit = allocatedDeposit,
                        PaidRemain = paidRemain,
                        TotalPaidForNote = totalPaidForNote,
                        NoteBalance = noteBalance
                    };

                    invoiceDetails.Add(detail);

                    invoice.TotalAmount += noteAmount;
                    invoice.TotalDeposit += allocatedDeposit;
                    invoice.TotalPaid += totalPaidForNote;

                    exportIndex++;
                }

                // 7. Cập nhật PaymentStatus + TotalRemain
                UpdateInvoicePaymentStatus(invoice);

                await _unitOfWork.InvoicesDetails.AddRangeAsync(invoiceDetails);
                _unitOfWork.Invoices.Update(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // 8. Map DTO trả về
                exportIndex = 1;
                var detailDtos = noteInfos.Select(x =>
                {
                    var note = x.Note;
                    var noteAmount = x.NoteAmount;

                    var proportion = noteAmount / orderTotal;
                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);
                    var paidRemain = 0m;
                    var totalPaidForNote = allocatedDeposit + paidRemain;
                    var noteBalance = noteAmount - totalPaidForNote;

                    return new InvoiceDetailDTO
                    {
                        GoodsIssueNoteId = note.Id,
                        GoodsIssueDate = note.DeliveryDate,
                        GoodsIssueAmount = noteAmount,
                        AllocatedDeposit = allocatedDeposit,
                        PaidRemain = paidRemain,
                        TotalPaidForNote = totalPaidForNote,
                        NoteBalance = noteBalance,
                        ExportIndex = exportIndex++
                    };
                }).ToList();

                var invoiceDto = new InvoiceDTO
                {
                    Id = invoice.Id,
                    InvoiceCode = invoice.InvoiceCode,
                    SalesOrderId = invoice.SalesOrderId,
                    CreatedAt = invoice.CreatedAt,
                    IssuedAt = invoice.IssuedAt,
                    Status = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    TotalPaid = invoice.TotalPaid,
                    TotalDeposit = invoice.TotalDeposit,
                    TotalRemain = invoice.TotalRemain,
                    Details = detailDtos
                };

                return new ServiceResult<InvoiceDTO>
                {
                    StatusCode = 201,
                    Message = "Tạo hóa đơn thành công.",
                    Data = invoiceDto
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi GenerateInvoiceFromGoodsIssueNotesAsync");

                return new ServiceResult<InvoiceDTO>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi tạo hóa đơn.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<InvoicePDFResultDTO>> GenerateInvoicePdfAsync(int invoiceId)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(d => d.GoodsIssueNote)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return ServiceResult<InvoicePDFResultDTO>.Fail("Không tìm thấy hóa đơn.", 404);

                var html = InvoiceTemplate.GenerateInvoiceHtml(invoice);
                var pdfBytes = _pdfService.GeneratePdfFromHtml(html);

                var result = new InvoicePDFResultDTO
                {
                    PdfBytes = pdfBytes,
                    FileName = $"{invoice.InvoiceCode}.pdf"
                };

                return ServiceResult<InvoicePDFResultDTO>.SuccessResult(result,
                    "Tạo PDF hóa đơn thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateInvoicePdfAsync({InvoiceId}) error", invoiceId);
                return ServiceResult<InvoicePDFResultDTO>.Fail(
                    "Có lỗi xảy ra khi tạo PDF hóa đơn.", 500);
            }
        }

        public async Task<ServiceResult<List<InvoiceDTO>>> GetAllInvoicesAsync()
        {
            try
            {
                var invoices = await _unitOfWork.Invoices.Query()
                    .Include(i => i.InvoiceDetails)
                    .ToListAsync();

                if (!invoices.Any())
                {
                    return ServiceResult<List<InvoiceDTO>>.Fail(
                        "Không có hóa đơn nào.", 404);
                }

                var result = invoices.Select(inv =>
                {

                    var latestExportedAt = inv.InvoiceDetails
                        .Select(d => d.GoodsIssueNote.DeliveryDate)
                        .Max();

                    var paymentDueAt = latestExportedAt.AddDays(3);

                    return new InvoiceDTO
                    {
                        Id = inv.Id,
                        InvoiceCode = inv.InvoiceCode,
                        SalesOrderId = inv.SalesOrderId,
                        SalesOrderCode = inv.SalesOrder.SalesOrderCode,
                        CreatedAt = inv.CreatedAt,
                        IssuedAt = inv.IssuedAt,
                        Status = inv.Status,
                        TotalAmount = inv.TotalAmount,
                        TotalPaid = inv.TotalPaid,
                        TotalDeposit = inv.TotalDeposit,
                        TotalRemain = inv.TotalRemain,
                        LatestExportedAt = latestExportedAt,
                        PaymentDueAt = paymentDueAt,
                        CustomerName = inv.SalesOrder.Customer.CustomerProfile.User.FullName,
                        Details = inv.InvoiceDetails.Select(d => new InvoiceDetailDTO
                        {
                            GoodsIssueNoteId = d.GoodsIssueNoteId,
                            GoodsIssueDate = d.GoodsIssueNote.DeliveryDate,
                            GoodsIssueAmount = d.GoodsIssueAmount,
                            AllocatedDeposit = d.AllocatedDeposit,
                            PaidRemain = d.PaidRemain,
                            TotalPaidForNote = d.TotalPaidForNote,
                            NoteBalance = d.NoteBalance
                        }).ToList()
                    };
                }).ToList();
                return ServiceResult<List<InvoiceDTO>>.SuccessResult(
                    result, "Lấy danh sách hóa đơn thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllInvoicesAsync error");
                return ServiceResult<List<InvoiceDTO>>.Fail(
                    "Có lỗi xảy ra khi lấy danh sách hóa đơn.", 500);
            }
        }

        public async Task<ServiceResult<InvoiceDTO>> GetInvoiceByIdAsync(int invoiceId)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(d => d.GoodsIssueNote)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return ServiceResult<InvoiceDTO>.Fail("Không tìm thấy hóa đơn.", 404);

                var latestExportedAt = invoice.InvoiceDetails
                        .Select(d => d.GoodsIssueNote.DeliveryDate)
                        .Max();

                var paymentDueAt = latestExportedAt.AddDays(3);
                var dto = new InvoiceDTO
                {
                    Id = invoice.Id,
                    InvoiceCode = invoice.InvoiceCode,
                    SalesOrderId = invoice.SalesOrderId,
                    SalesOrderCode = invoice.SalesOrder.SalesOrderCode,
                    CreatedAt = invoice.CreatedAt,
                    IssuedAt = invoice.IssuedAt,
                    Status = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    TotalPaid = invoice.TotalPaid,
                    TotalDeposit = invoice.TotalDeposit,
                    TotalRemain = invoice.TotalRemain,
                    LatestExportedAt = latestExportedAt,
                    PaymentDueAt = paymentDueAt,
                    CustomerName = invoice.SalesOrder.Customer.CustomerProfile.User.FullName,
                    Details = invoice.InvoiceDetails.Select((d, index) => new InvoiceDetailDTO
                    {
                        GoodsIssueNoteId = d.GoodsIssueNoteId,
                        GoodsIssueDate = d.GoodsIssueNote.DeliveryDate,
                        GoodsIssueAmount = d.GoodsIssueAmount,
                        AllocatedDeposit = d.AllocatedDeposit,
                        PaidRemain = d.PaidRemain,
                        TotalPaidForNote = d.TotalPaidForNote,
                        NoteBalance = d.NoteBalance,
                        ExportIndex = index + 1
                    }).ToList()
                };

                return ServiceResult<InvoiceDTO>.SuccessResult(
                    dto, "Lấy chi tiết hóa đơn thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetInvoiceByIdAsync({InvoiceId}) error", invoiceId);
                return ServiceResult<InvoiceDTO>.Fail(
                    "Có lỗi xảy ra khi lấy chi tiết hóa đơn.", 500);
            }
        }

        public async Task<ServiceResult<bool>> SendInvoiceEmailAsync(int invoiceId, string currentUserId)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(d => d.GoodsIssueNote)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy hóa đơn.", 404);

                var customer = invoice.SalesOrder.Customer;
                if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
                    return ServiceResult<bool>.Fail("Không tìm thấy email khách hàng.", 400);

                // render HTML + PDF
                var html = InvoiceTemplate.GenerateInvoiceHtml(invoice);
                var pdfBytes = _pdfService.GeneratePdfFromHtml(html);
                var fileName = $"{invoice.InvoiceCode}.pdf";

                var customerName = customer.FullName ?? customer.Email!;
                var subject = EmailSubject.Invoice;
                var body = EmailBody.INVOICE(customerName);

                await _emailService.SendMailWithPDFAsync(
                    subject,
                    body,
                    customer.Email!,
                    pdfBytes,
                    fileName);

                // cập nhật trạng thái hóa đơn
                invoice.Status = InvoiceStatus.Send;
                _unitOfWork.Invoices.Update(invoice);

                try
                {
                    var senderId = currentUserId; 
                    if (!string.IsNullOrEmpty(senderId))
                    {
                        await _noti.SendNotificationToCustomerAsync(
                            senderId: senderId,
                            receiverId: customer.Id,
                            title: "Hóa đơn đã được gửi",
                            message: $"Hóa đơn {invoice.InvoiceCode} của bạn đã được gửi tới email {customer.Email}.",
                            type: NotificationType.Message
                        );
                    }
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification hóa đơn thất bại: invoiceId={InvoiceId}, receiver={ReceiverId}",
                        invoice.Id, customer.Id);
                }
                await _unitOfWork.CommitAsync();

                return ServiceResult<bool>.SuccessResult(true,
                    "Gửi hóa đơn thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendInvoiceEmailAsync({InvoiceId}) error", invoiceId);
                return ServiceResult<bool>.Fail(
                    "Có lỗi xảy ra khi gửi hóa đơn.", 500);
            }
        }

        public async Task<ServiceResult<InvoiceDTO>> UpdateInvoiceGoodsIssueNotesAsync(int invoiceId, InvoiceUpdateDTO request)
        {
            try
            {
                if (request == null || request.GoodsIssueNoteCodes == null || request.GoodsIssueNoteCodes.Count == 0)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "Danh sách GoodsIssueNoteCodes trống.",
                        Data = null
                    };
                }

                // Lấy invoice + SalesOrder + SalesQuotation + SalesOrderDetails + InvoiceDetails
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(o => o.SalesQuotation)
                    .Include(i => i.SalesOrder.SalesOrderDetails)
                    .Include(i => i.InvoiceDetails)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy hóa đơn.",
                        Data = null
                    };
                }

                if (invoice.Status != InvoiceStatus.Draft)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "Chỉ được sửa hóa đơn khi còn ở trạng thái Draft.",
                        Data = null
                    };
                }

                var order = invoice.SalesOrder;
                if (order == null)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "Hóa đơn không gắn với SalesOrder hợp lệ.",
                        Data = null
                    };
                }

                if (order.SalesQuotation == null)
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "SalesOrder chưa gắn với SalesQuotation, không xác định được % cọc.",
                        Data = null
                    };
                }

                // Lấy GoodsIssueNote theo Code
                var goodsIssueNotes = await _unitOfWork.GoodsIssueNote.Query()
                    .Include(n => n.StockExportOrder)
                    .Include(n => n.GoodsIssueNoteDetails)
                    .Where(n => request.GoodsIssueNoteCodes.Contains(n.GoodsIssueNoteCode))
                    .ToListAsync();

                if (!goodsIssueNotes.Any())
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy GoodsIssueNote tương ứng với các mã đã gửi.",
                        Data = null
                    };
                }

                if (goodsIssueNotes.Any(n => n.StockExportOrder.SalesOrderId != order.SalesOrderId))
                {
                    return new ServiceResult<InvoiceDTO>
                    {
                        StatusCode = 400,
                        Message = "Có GoodsIssueNote không thuộc cùng SalesOrder với hóa đơn.",
                        Data = null
                    };
                }

                var orderTotal = order.TotalPrice;

                // Tổng cọc cố định theo SalesOrder (% trong SalesQuotation)
                var depositFixed = decimal.Round(
                    orderTotal * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                // Tính giá trị từng phiếu xuất
                var noteInfos = new List<(PMS.Core.Domain.Entities.GoodsIssueNote Note, decimal NoteAmount)>();

                foreach (var note in goodsIssueNotes)
                {
                    decimal noteAmount = 0m;

                    foreach (var d in note.GoodsIssueNoteDetails)
                    {
                        var soDetail = order.SalesOrderDetails
                            .FirstOrDefault(x => x.LotId == d.LotId);

                        if (soDetail == null)
                        {
                            return new ServiceResult<InvoiceDTO>
                            {
                                StatusCode = 400,
                                Message = $"Không tìm thấy SalesOrderDetail cho LotId={d.LotId}.",
                                Data = null
                            };
                        }

                        noteAmount += soDetail.UnitPrice * d.Quantity;
                    }

                    noteInfos.Add((note, noteAmount));
                }

                noteInfos = noteInfos
                    .OrderBy(x => x.Note.DeliveryDate)
                    .ThenBy(x => x.Note.Id)
                    .ToList();

                await _unitOfWork.BeginTransactionAsync();

                var oldDetails = await _unitOfWork.InvoicesDetails.Query()
                    .Where(d => d.InvoiceId == invoice.Id)
                    .ToListAsync();

                if (oldDetails.Any())
                {
                    _unitOfWork.InvoicesDetails.RemoveRange(oldDetails);
                }

                invoice.TotalAmount = 0m;
                invoice.TotalPaid = 0m;
                invoice.TotalDeposit = 0m;
                invoice.TotalRemain = 0m;
                invoice.PaymentStatus = PaymentStatus.NotPaymentYet;

                var tmpReq = new GenerateInvoiceFromGINRequestDTO
                {
                    SalesOrderCode = order.SalesOrderCode,
                    GoodsIssueNoteCodes = request.GoodsIssueNoteCodes
                };
                invoice.InvoiceCode = BuildInvoiceCode(tmpReq);

                var newDetails = new List<InvoiceDetail>();
                int exportIndex = 1;

                foreach (var (note, noteAmount) in noteInfos)
                {
                    var proportion = orderTotal > 0
                        ? noteAmount / orderTotal
                        : 0m;

                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);

                    var paidRemain = 0m; 
                    var totalPaidForNote = allocatedDeposit + paidRemain;
                    var noteBalance = noteAmount - totalPaidForNote;

                    var detail = new InvoiceDetail
                    {
                        InvoiceId = invoice.Id,
                        GoodsIssueNoteId = note.Id,
                        GoodsIssueAmount = noteAmount,
                        AllocatedDeposit = allocatedDeposit,
                        PaidRemain = paidRemain,
                        TotalPaidForNote = totalPaidForNote,
                        NoteBalance = noteBalance
                    };

                    newDetails.Add(detail);

                    invoice.TotalAmount += noteAmount;
                    invoice.TotalDeposit += allocatedDeposit;
                    invoice.TotalPaid += totalPaidForNote;

                    exportIndex++;
                }

                // Cập nhật TotalRemain + PaymentStatus
                UpdateInvoicePaymentStatus(invoice);

                await _unitOfWork.InvoicesDetails.AddRangeAsync(newDetails);
                _unitOfWork.Invoices.Update(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Map DTO trả về
                exportIndex = 1;
                var detailDtos = noteInfos.Select(x =>
                {
                    var note = x.Note;
                    var noteAmount = x.NoteAmount;

                    var proportion = orderTotal > 0
                        ? noteAmount / orderTotal
                        : 0m;

                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);
                    var paidRemain = 0m;
                    var totalPaidForNote = allocatedDeposit + paidRemain;
                    var noteBalance = noteAmount - totalPaidForNote;

                    return new InvoiceDetailDTO
                    {
                        GoodsIssueNoteId = note.Id,
                        GoodsIssueDate = note.DeliveryDate,
                        GoodsIssueAmount = noteAmount,
                        AllocatedDeposit = allocatedDeposit,
                        PaidRemain = paidRemain,
                        TotalPaidForNote = totalPaidForNote,
                        NoteBalance = noteBalance,
                        ExportIndex = exportIndex++
                    };
                }).ToList();

                var invoiceDto = new InvoiceDTO
                {
                    Id = invoice.Id,
                    InvoiceCode = invoice.InvoiceCode,
                    SalesOrderId = invoice.SalesOrderId,
                    CreatedAt = invoice.CreatedAt,
                    IssuedAt = invoice.IssuedAt,
                    Status = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    TotalPaid = invoice.TotalPaid,
                    TotalDeposit = invoice.TotalDeposit,
                    TotalRemain = invoice.TotalRemain,
                    Details = detailDtos
                };

                return new ServiceResult<InvoiceDTO>
                {
                    StatusCode = 200,
                    Message = "Cập nhật hóa đơn theo danh sách GoodsIssueNoteCodes thành công.",
                    Data = invoiceDto
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "UpdateInvoiceGoodsIssueNotesAsync({InvoiceId}) error", invoiceId);

                return new ServiceResult<InvoiceDTO>
                {
                    StatusCode = 500,
                    Message = "Có lỗi xảy ra khi cập nhật hóa đơn.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Lấy toàn bộ SalesOrderCode
        /// </summary>
        /// <returns></returns>
        public async Task<ServiceResult<List<string>>> GetAllSalesOrderCodesAsync()
        {
            try
            {
                var codes = await _unitOfWork.GoodsIssueNote.Query()
                    .Where(g => g.StockExportOrder.SalesOrder != null && !string.IsNullOrEmpty(g.StockExportOrder.SalesOrder.SalesOrderCode))
                    .Where(g => g.Status == GoodsIssueNoteStatus.Exported)
                    .Where(g => !g.InvoiceDetails.Any())
                    .Select(g => g.StockExportOrder.SalesOrder!.SalesOrderCode!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                if (!codes.Any())
                {
                    return ServiceResult<List<string>>.Fail(
                        "Không tìm thấy đơn hàng nào.", 404);
                }

                return ServiceResult<List<string>>.SuccessResult(
                    codes,
                    "Lấy danh sách mã đơn hàng thành công.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllSalesOrderCodesAsync error");
                return ServiceResult<List<string>>.Fail(
                    "Có lỗi xảy ra khi lấy danh sách mã đơn hàng.", 500);
            }
        }

        /// <summary>
        /// Lấy toàn bộ GoodsIssueNoteCode theo SalesOrderCode
        /// </summary>
        public async Task<ServiceResult<List<string>>> GetGoodsIssueNoteCodesBySalesOrderCodeAsync(string salesOrderCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(salesOrderCode))
                {
                    return ServiceResult<List<string>>.Fail(
                        "Mã đơn hàng không hợp lệ.", 400);
                }

                // Tìm SalesOrder theo code
                var order = await _unitOfWork.SalesOrder.Query()
                    .FirstOrDefaultAsync(o => o.SalesOrderCode == salesOrderCode);

                if (order == null)
                {
                    return ServiceResult<List<string>>.Fail(
                        "Không tìm thấy đơn hàng với mã đã cung cấp.", 404);
                }

                var noteCodes = await _unitOfWork.GoodsIssueNote.Query()
                    .Include(n => n.StockExportOrder)
                    .Where(n => n.StockExportOrder.SalesOrderId == order.SalesOrderId)
                    .Where(n => !string.IsNullOrEmpty(n.GoodsIssueNoteCode))
                    .Where(n => !n.InvoiceDetails.Any())
                    .Where(n => n.Status == GoodsIssueNoteStatus.Exported)
                    .Select(n => n.GoodsIssueNoteCode!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                if (!noteCodes.Any())
                {
                    return ServiceResult<List<string>>.Fail(
                        "Không tìm thấy phiếu xuất nào thuộc đơn hàng này.", 404);
                }

                return ServiceResult<List<string>>.SuccessResult(
                    noteCodes,
                    "Lấy danh sách mã phiếu xuất theo mã đơn hàng thành công.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetGoodsIssueNoteCodesBySalesOrderCodeAsync({SalesOrderCode}) error", salesOrderCode);
                return ServiceResult<List<string>>.Fail(
                    "Có lỗi xảy ra khi lấy danh sách mã phiếu xuất.", 500);
            }
        }

        public async Task<ServiceResult<List<InvoiceDTO>>> GetInvoicesForCurrentCustomerAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResult<List<InvoiceDTO>>.Fail(
                        "UserId không hợp lệ.", 400);
                }

                var invoices = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.Customer)  
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(d => d.GoodsIssueNote)
                    .Where(i => i.SalesOrder.CreateBy == userId && i.Status == InvoiceStatus.Send)
                    .ToListAsync();

                if (!invoices.Any())
                {
                    return ServiceResult<List<InvoiceDTO>>.Fail(
                        "Khách hàng này chưa có hóa đơn nào.", 404);
                }

                var result = invoices.Select(inv =>
                {
                    var latestExportedAt = inv.InvoiceDetails
                        .Select(d => d.GoodsIssueNote.DeliveryDate)
                        .Max();

                    var paymentDueAt = latestExportedAt.AddDays(3);

                    return new InvoiceDTO
                    {
                        Id = inv.Id,
                        InvoiceCode = inv.InvoiceCode,
                        SalesOrderId = inv.SalesOrderId,
                        SalesOrderCode = inv.SalesOrder.SalesOrderCode,
                        CreatedAt = inv.CreatedAt,
                        IssuedAt = inv.IssuedAt,
                        Status = inv.Status,
                        TotalAmount = inv.TotalAmount,
                        TotalPaid = inv.TotalPaid,
                        TotalDeposit = inv.TotalDeposit,
                        TotalRemain = inv.TotalRemain,
                        LatestExportedAt = latestExportedAt,
                        PaymentDueAt = paymentDueAt,
                        CustomerName = inv.SalesOrder.Customer.CustomerProfile.User.FullName,
                        Details = inv.InvoiceDetails
                        .Select((d, index) => new InvoiceDetailDTO
                        {
                            GoodsIssueNoteId = d.GoodsIssueNoteId,
                            GoodsIssueDate = d.GoodsIssueNote.DeliveryDate,
                            GoodsIssueAmount = d.GoodsIssueAmount,
                            AllocatedDeposit = d.AllocatedDeposit,
                            PaidRemain = d.PaidRemain,
                            TotalPaidForNote = d.TotalPaidForNote,
                            NoteBalance = d.NoteBalance,
                            ExportIndex = index + 1
                        }).ToList()
                    };
                }).ToList();

                return ServiceResult<List<InvoiceDTO>>.SuccessResult(
                    result,
                    "Lấy danh sách hóa đơn cho customer hiện tại thành công.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetInvoicesForCurrentCustomerAsync({UserId}) error", userId);
                return ServiceResult<List<InvoiceDTO>>.Fail(
                    "Có lỗi xảy ra khi lấy danh sách hóa đơn của customer.", 500);
            }
        }

        public async Task<ServiceResult<InvoiceSmartCASignResponseDTO>> CreateSmartCASignTransactionAsync(int invoiceId, SmartCASignInvoiceRequestDTO request)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(d => d.GoodsIssueNote)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return ServiceResult<InvoiceSmartCASignResponseDTO>
                        .Fail("Không tìm thấy hóa đơn.", 404);

                // 1. Render PDF (giống GenerateInvoicePdfAsync)
                var html = InvoiceTemplate.GenerateInvoiceHtml(invoice);
                var pdfBytes = _pdfService.GeneratePdfFromHtml(html);

                // 2. Gọi SmartCA tạo giao dịch ký hash
                var docId = invoice.InvoiceCode; // hoặc bất kỳ mã tài liệu nào anh muốn
                var signResult = await _smartCAService.SignPdfHashAsync(
                    pdfBytes,
                    docId,
                    request);

                // TODO: Lưu transactionId / tranCode vào Invoice nếu muốn
                // invoice.SmartCATransactionId = signResult.TransactionId;
                // _unitOfWork.Invoices.Update(invoice);
                // await _unitOfWork.CommitAsync();

                var dto = new InvoiceSmartCASignResponseDTO
                {
                    InvoiceId = invoice.Id,
                    InvoiceCode = invoice.InvoiceCode,
                    TransactionId = signResult.TransactionId,
                    TranCode = signResult.TranCode
                };

                return ServiceResult<InvoiceSmartCASignResponseDTO>.SuccessResult(
                    dto,
                    "Tạo giao dịch ký số SmartCA thành công. Vui lòng xác nhận trên app SmartCA.",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateSmartCASignTransactionAsync({InvoiceId}) error", invoiceId);
                return ServiceResult<InvoiceSmartCASignResponseDTO>.Fail(
                    "Có lỗi xảy ra khi tạo giao dịch ký số.", 500);
            }
        }

        public async Task<ServiceResult<bool>> DeleteDraftInvoiceAsync(int invoiceId)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.InvoiceDetails)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                {
                    return ServiceResult<bool>.Fail(
                        "Không tìm thấy hóa đơn.", 404);
                }

                if (invoice.Status != InvoiceStatus.Draft)
                {
                    return ServiceResult<bool>.Fail(
                        "Chỉ được phép xóa hóa đơn khi đang ở trạng thái nháp.", 400);
                }

                await _unitOfWork.BeginTransactionAsync();

                if (invoice.InvoiceDetails.Any())
                {
                    _unitOfWork.InvoicesDetails.RemoveRange(invoice.InvoiceDetails);
                }

                _unitOfWork.Invoices.Remove(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<bool>.SuccessResult(
                    true,
                    "Xóa hóa đơn nháp thành công.",
                    200);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "DeleteDraftInvoiceAsync({InvoiceId}) error", invoiceId);

                return ServiceResult<bool>.Fail(
                    "Có lỗi xảy ra khi xóa hóa đơn.", 500);
            }
        }

        public async Task<ServiceResult<bool>> SendLateReminderEmailAsync(int invoiceId, string currentUserId)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(d => d.GoodsIssueNote)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return ServiceResult<bool>.Fail("Không tìm thấy hóa đơn.", 404);

                if (invoice.PaymentStatus != PaymentStatus.Late)
                    return ServiceResult<bool>.Fail("Hóa đơn này không ở trạng thái quá hạn.", 400);

                if (invoice.TotalRemain <= 0)
                    return ServiceResult<bool>.Fail("Hóa đơn này đã được thanh toán đủ, không cần nhắc.", 400);

                var customer = invoice.SalesOrder.Customer;
                if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
                    return ServiceResult<bool>.Fail("Không tìm thấy email khách hàng.", 400);

                // Link để nhấn vào invoiceCode (bạn đổi theo frontend thật)
                var invoiceUrl = $"https://bbpharmacy.site/invoices/{invoice.Id}";

                // body mail (không nhét string trong service)
                var customerName = customer.FullName ?? customer.Email!;
                var subject = EmailSubject.InvoiceLateReminder;
                var body = EmailBody.INVOICE_LATE_REMINDER(customerName, invoice.InvoiceCode, invoiceUrl, invoice.TotalRemain);

                // PDF đính kèm từ template riêng
                var html = InvoiceTemplate.GenerateLateReminderHtml(invoice);
                var pdfBytes = _pdfService.GeneratePdfFromHtml(html);
                var fileName = $"NHAC_THANH_TOAN_{invoice.InvoiceCode}.pdf";

                await _emailService.SendMailWithPDFAsync(
                    subject,
                    body,
                    customer.Email!,
                    pdfBytes,
                    fileName);

                // Noti cho khách (tuỳ bạn)
                try
                {
                    var senderId = currentUserId;
                    if (!string.IsNullOrEmpty(senderId))
                    {
                        await _noti.SendNotificationToCustomerAsync(
                            senderId: senderId,
                            receiverId: customer.Id,
                            title: "Nhắc thanh toán hóa đơn quá hạn",
                            message: $"Hóa đơn {invoice.InvoiceCode} của bạn đã quá hạn. Vui lòng kiểm tra email để thanh toán.",
                            type: NotificationType.Warning
                        );
                    }
                }
                catch (Exception exNotify)
                {
                    _logger.LogWarning(exNotify,
                        "Gửi notification nhắc thanh toán thất bại: invoiceId={InvoiceId}, receiver={ReceiverId}",
                        invoice.Id, customer.Id);
                }

                await _unitOfWork.CommitAsync();

                return ServiceResult<bool>.SuccessResult(true, "Đã gửi email nhắc thanh toán hóa đơn quá hạn.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendLateReminderEmailAsync({InvoiceId}) error", invoiceId);
                return ServiceResult<bool>.Fail("Có lỗi xảy ra khi gửi email nhắc thanh toán.", 500);
            }
        }


        #region Helpers

        private static void UpdateInvoicePaymentStatus(Core.Domain.Entities.Invoice invoice)
        {
            // TotalRemain luôn = TotalAmount - TotalPaid
            invoice.TotalRemain = invoice.TotalAmount - invoice.TotalPaid;

            if (invoice.TotalPaid <= 0)
            {
                invoice.PaymentStatus = PaymentStatus.NotPaymentYet;
            }
            else if (invoice.TotalPaid < invoice.TotalAmount)
            {
                invoice.PaymentStatus = PaymentStatus.Deposited;
            }
            else
            {
                invoice.PaymentStatus = PaymentStatus.Paid;
            }
        }

        private static string BuildInvoiceCode(GenerateInvoiceFromGINRequestDTO request)
        {
            var firstNoteCode = request.GoodsIssueNoteCodes.First();
            var noteCount = request.GoodsIssueNoteCodes.Count;

            string rawCode;

            if (noteCount == 1)
            {
                // 1 phiếu xuất
                rawCode = $"INV-{request.SalesOrderCode}-{firstNoteCode}";
            }
            else
            {
                // Nhiều phiếu: lấy code phiếu đầu + số lượng
                rawCode = $"INV-{request.SalesOrderCode}-{firstNoteCode}-x{noteCount}";
            }

            // Đảm bảo không vượt quá 50 ký tự (Fluent đang set HasMaxLength(50))
            return rawCode.Length > 50
                ? rawCode.Substring(0, 50)
                : rawCode;
        }

        #endregion

    }
}
