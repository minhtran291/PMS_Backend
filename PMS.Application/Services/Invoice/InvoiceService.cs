using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Invoice;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
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
        IEmailService emailService) : Service(unitOfWork, mapper), IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<InvoiceService> _logger = logger;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IEmailService _emailService = emailService;

        public async Task<ServiceResult<InvoiceDTO>>
            GenerateInvoiceFromPaymentRemainsAsync(GenerateInvoiceFromPaymentRemainsRequestDTO request)
        {
            try
            {
                if (request.PaymentRemainIds == null || request.PaymentRemainIds.Count == 0)
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Danh sách PaymentRemainIds trống.", 400);
                }

                // Load PaymentRemain + điều kiện
                var payments = await _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.SalesOrder)
                        .ThenInclude(o => o.SalesQuotation)
                    .Include(p => p.GoodsIssueNote)
                        .ThenInclude(n => n.GoodsIssueNoteDetails)
                    .Include(p => p.SalesOrder.SalesOrderDetails)
                    .Where(p => request.PaymentRemainIds.Contains(p.Id))
                    .ToListAsync();

                if (!payments.Any())
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Không tìm thấy PaymentRemain tương ứng.", 404);
                }

                // Validate cùng 1 SalesOrder
                var soId = payments.First().SalesOrderId;
                if (soId != request.SalesOrderId ||
                    payments.Any(p => p.SalesOrderId != soId))
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Các PaymentRemain không thuộc cùng một SalesOrder.", 400);
                }

                var order = payments.First().SalesOrder;

                if (order.SalesQuotation == null)
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "SalesOrder chưa gắn với SalesQuotation, không xác định được % cọc.", 400);
                }

                // Chỉ lấy PaymentRemain đã Success
                if (payments.Any(p => p.Status != PaymentStatus.Success))
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Chỉ generate Invoice cho PaymentRemain ở trạng thái Success.", 400);
                }

                // Tính toán tổng cọc cố định theo SalesOrder
                var orderTotal = order.TotalPrice;
                var depositFixed = decimal.Round(
                    orderTotal * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                // Group theo GoodsIssueNote, sắp xếp theo ngày xuất
                var noteGroups = payments
                    .GroupBy(p => p.GoodsIssueNoteId)
                    .ToList();

                var noteInfos = new List<(PMS.Core.Domain.Entities.GoodsIssueNote Note, PaymentRemain Payment, decimal NoteAmount)>();

                foreach (var g in noteGroups)
                {
                    var payment = g.First();

                    if (payment.GoodsIssueNote == null)
                    {
                        return ServiceResult<InvoiceDTO>.Fail(
                            "PaymentRemain không gắn với GoodsIssueNote.", 400);
                    }

                    var note = payment.GoodsIssueNote;

                    // Giá trị phiếu xuất = sum(UnitPrice * quantity) theo LotId
                    decimal noteAmount = 0m;
                    foreach (var d in note.GoodsIssueNoteDetails)
                    {
                        var soDetail = order.SalesOrderDetails
                            .FirstOrDefault(x => x.LotId == d.LotId);

                        if (soDetail == null)
                        {
                            return ServiceResult<InvoiceDTO>.Fail(
                                $"Không tìm thấy SalesOrderDetail cho LotId={d.LotId}.", 400);
                        }

                        noteAmount += soDetail.UnitPrice * d.Quantity;
                    }

                    noteInfos.Add((note, payment, noteAmount));
                }

                // Sort theo ngày xuất + Id → tạo "Lần xuất hàng"
                noteInfos = noteInfos
                    .OrderBy(x => x.Note.DeliveryDate) 
                    .ThenBy(x => x.Note.Id)
                    .ToList();

                // Tạo Invoice + InvoiceDetails
                await _unitOfWork.BeginTransactionAsync();

                var now = DateTime.Now;
                var invoice = new PMS.Core.Domain.Entities.Invoice
                {
                    SalesOrderId = order.SalesOrderId,
                    InvoiceCode = $"INV-SO{order.SalesOrderId}-{now:ddMMyyyyHHmmss}",
                    CreatedAt = now,
                    IssuedAt = now,
                    Status = InvoiceStatus.Draft, 
                    TotalAmount = 0m,
                    TotalPaid = 0m,
                    TotalDeposit = 0m,
                    TotalRemain = 0m
                };

                await _unitOfWork.Invoices.AddAsync(invoice);
                await _unitOfWork.CommitAsync();

                var invoiceDetails = new List<InvoiceDetail>();

                int exportIndex = 1;

                foreach (var (note, payment, noteAmount) in noteInfos)
                {
                    var proportion = noteAmount / orderTotal;

                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);

                    var paidRemain = payment.Amount;

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

                    // Cộng vào tổng Invoice
                    invoice.TotalAmount += noteAmount;
                    invoice.TotalDeposit += allocatedDeposit;
                    invoice.TotalRemain += paidRemain;
                    invoice.TotalPaid += totalPaidForNote;

                    exportIndex++;
                }

                await _unitOfWork.InvoicesDetails.AddRangeAsync(invoiceDetails);
                _unitOfWork.Invoices.Update(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Map sang DTO cho FE
                exportIndex = 1;
                var detailDtos = noteInfos.Select(x =>
                {
                    var note = x.Note;
                    var payment = x.Payment;
                    var noteAmount = x.NoteAmount;

                    var proportion = noteAmount / orderTotal;
                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);
                    var paidRemain = payment.Amount;
                    var totalPaidForNote = allocatedDeposit + paidRemain;
                    var noteBalance = noteAmount - totalPaidForNote;

                    var dto = new InvoiceDetailDTO
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

                    return dto;
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

                return ServiceResult<InvoiceDTO>.SuccessResult(
                    invoiceDto,
                    "Tạo hóa đơn thành công.",
                    201);
            } 
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi GenerateInvoiceFromPaymentRemainsAsync");
                return ServiceResult<InvoiceDTO>.Fail(
                    "Có lỗi xảy ra khi tạo hóa đơn.", 500);
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

                var result = invoices.Select(i => new InvoiceDTO
                {
                    Id = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    SalesOrderId = i.SalesOrderId,
                    CreatedAt = i.CreatedAt,
                    IssuedAt = i.IssuedAt,
                    Status = i.Status,
                    TotalAmount = i.TotalAmount,
                    TotalPaid = i.TotalPaid,
                    TotalDeposit = i.TotalDeposit,
                    TotalRemain = i.TotalRemain,
                    Details = i.InvoiceDetails.Select(d => new InvoiceDetailDTO
                    {
                        GoodsIssueNoteId = d.GoodsIssueNoteId,
                        GoodsIssueDate = d.GoodsIssueNote.DeliveryDate,
                        GoodsIssueAmount = d.GoodsIssueAmount,
                        AllocatedDeposit = d.AllocatedDeposit,
                        PaidRemain = d.PaidRemain,
                        TotalPaidForNote = d.TotalPaidForNote,
                        NoteBalance = d.NoteBalance
                    }).ToList()
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

                var dto = new InvoiceDTO
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

        public async Task<ServiceResult<bool>> SendInvoiceEmailAsync(int invoiceId)
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

        public async Task<ServiceResult<InvoiceDTO>> UpdateInvoicePaymentRemainsAsync(int invoiceId, InvoiceUpdateDTO request)
        {
            try
            {
                if (request.PaymentRemainIds == null || request.PaymentRemainIds.Count == 0)
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Danh sách PaymentRemainIds trống.", 400);
                }

                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.SalesOrder)
                        .ThenInclude(o => o.SalesQuotation)
                    .Include(i => i.SalesOrder.SalesOrderDetails)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return ServiceResult<InvoiceDTO>.Fail("Không tìm thấy hóa đơn.", 404);

                if (invoice.Status != InvoiceStatus.Draft)
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Chỉ được sửa hóa đơn khi còn ở trạng thái Draft.", 400);
                }

                var order = invoice.SalesOrder;

                if (order.SalesQuotation == null)
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "SalesOrder chưa gắn với SalesQuotation, không xác định được % cọc.", 400);
                }

                // Lấy lại list PaymentRemain theo Id vừa gửi
                var payments = await _unitOfWork.PaymentRemains.Query()
                    .Include(p => p.GoodsIssueNote)
                        .ThenInclude(n => n.GoodsIssueNoteDetails)
                    .Where(p => request.PaymentRemainIds.Contains(p.Id))
                    .ToListAsync();

                if (!payments.Any())
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Không tìm thấy PaymentRemain tương ứng.", 404);
                }

                // Validate cùng salesOrder
                if (payments.Any(p => p.SalesOrderId != order.SalesOrderId))
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Các PaymentRemain không thuộc cùng một SalesOrder.", 400);
                }

                // Chỉ cho PaymentRemain Success
                if (payments.Any(p => p.Status != PaymentStatus.Success))
                {
                    return ServiceResult<InvoiceDTO>.Fail(
                        "Chỉ cho phép sửa hóa đơn với PaymentRemain ở trạng thái Success.", 400);
                }

                var orderTotal = order.TotalPrice;
                var depositFixed = decimal.Round(
                    orderTotal * (order.SalesQuotation.DepositPercent / 100m),
                    0,
                    MidpointRounding.AwayFromZero);

                var noteGroups = payments.GroupBy(p => p.GoodsIssueNoteId).ToList();
                var noteInfos = new List<(PMS.Core.Domain.Entities.GoodsIssueNote Note, PaymentRemain Payment, decimal NoteAmount)>();

                foreach (var g in noteGroups)
                {
                    var payment = g.First();

                    if (payment.GoodsIssueNote == null)
                    {
                        return ServiceResult<InvoiceDTO>.Fail(
                            "PaymentRemain không gắn với GoodsIssueNote.", 400);
                    }

                    var note = payment.GoodsIssueNote;

                    decimal noteAmount = 0m;
                    foreach (var d in note.GoodsIssueNoteDetails)
                    {
                        var soDetail = order.SalesOrderDetails
                            .FirstOrDefault(x => x.LotId == d.LotId);

                        if (soDetail == null)
                        {
                            return ServiceResult<InvoiceDTO>.Fail(
                                $"Không tìm thấy SalesOrderDetail cho LotId={d.LotId}.", 400);
                        }

                        noteAmount += soDetail.UnitPrice * d.Quantity;
                    }

                    noteInfos.Add((note, payment, noteAmount));
                }

                noteInfos = noteInfos
                    .OrderBy(x => x.Note.DeliveryDate)
                    .ThenBy(x => x.Note.Id)
                    .ToList();

                await _unitOfWork.BeginTransactionAsync();

                // Xoá toàn bộ InvoiceDetail cũ
                var oldDetails = await _unitOfWork.InvoicesDetails.Query()
                    .Where(d => d.InvoiceId == invoice.Id)
                    .ToListAsync();

                if (oldDetails.Any())
                    _unitOfWork.InvoicesDetails.RemoveRange(oldDetails);

                // Reset tổng
                invoice.TotalAmount = 0m;
                invoice.TotalPaid = 0m;
                invoice.TotalDeposit = 0m;
                invoice.TotalRemain = 0m;

                var newDetails = new List<InvoiceDetail>();
                int exportIndex = 1;

                foreach (var (note, payment, noteAmount) in noteInfos)
                {
                    var proportion = noteAmount / orderTotal;
                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);

                    var paidRemain = payment.Amount;
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
                    invoice.TotalRemain += paidRemain;
                    invoice.TotalPaid += totalPaidForNote;

                    exportIndex++;
                }

                await _unitOfWork.InvoicesDetails.AddRangeAsync(newDetails);
                _unitOfWork.Invoices.Update(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Map DTO trả về
                exportIndex = 1;
                var detailDtos = noteInfos.Select(x =>
                {
                    var note = x.Note;
                    var payment = x.Payment;
                    var noteAmount = x.NoteAmount;

                    var proportion = noteAmount / orderTotal;
                    var allocatedDeposit = decimal.Round(
                        depositFixed * proportion,
                        0,
                        MidpointRounding.AwayFromZero);
                    var paidRemain = payment.Amount;
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

                return ServiceResult<InvoiceDTO>.SuccessResult(
                    invoiceDto, "Cập nhật hóa đơn thành công.", 200);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "UpdateInvoicePaymentRemainsAsync({InvoiceId}) error", invoiceId);
                return ServiceResult<InvoiceDTO>.Fail(
                    "Có lỗi xảy ra khi cập nhật hóa đơn.", 500);
            }
        }
    }
}
