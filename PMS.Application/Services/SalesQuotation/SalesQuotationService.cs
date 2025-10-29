﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.SalesQuotation
{
    public class SalesQuotationService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SalesQuotationService> logger,
        IPdfService pdfService,
        IEmailService emailService,
        INotificationService notificationService) : Service(unitOfWork, mapper), ISalesQuotationService
    {
        private readonly ILogger<SalesQuotationService> _logger = logger;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IEmailService _emailService = emailService;
        private readonly INotificationService _notificationService = notificationService;

        public async Task<ServiceResult<object>> GenerateFormAsync(int rsqId)
        {
            try
            {
                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.RequestSalesQuotationDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == rsqId);

                var rsqValidation = ValidateRequestSalesQuotation(rsq);
                if (rsqValidation != null)
                    return rsqValidation;

                var productIds = rsq.RequestSalesQuotationDetails
                    .Select(r => r.ProductId)
                    .ToList();

                var listLot = await _unitOfWork.LotProduct.Query()
                    .Include(lp => lp.Product)
                    .AsNoTracking()
                    .Where(lp => productIds.Contains(lp.ProductID)
                                && lp.ExpiredDate > DateTime.Now
                                && lp.LotQuantity > 0)
                    .ToListAsync();

                var listTax = await _unitOfWork.TaxPolicy.Query()
                    .AsNoTracking()
                    .Where(tp => tp.Status == true)
                    .ToListAsync();

                var listNote = await _unitOfWork.SalesQuotationNote.Query()
                    .AsNoTracking()
                    .Where(sqn => sqn.IsActive == true)
                    .ToListAsync();

                var lotLookup = listLot.GroupBy(l => l.ProductID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var lotDtos = new List<LotDTO>();

                foreach (var detail in rsq.RequestSalesQuotationDetails)
                {
                    if (lotLookup.TryGetValue(detail.ProductId, out var lots))
                    {
                        lotDtos.AddRange(_mapper.Map<List<LotDTO>>(lots));
                    }
                    else
                    {
                        lotDtos.Add(new LotDTO
                        {
                            ProductID = detail.ProductId,
                            ProductName = detail.Product.ProductName,
                            Unit = detail.Product.Unit,
                            Note = "Hết hàng"
                        });
                    }
                }

                var taxDtos = _mapper.Map<List<TaxPolicyDTO>>(listTax);
                var noteDtos = _mapper.Map<List<SalesQuotationNoteDTO>>(listNote);

                var form = new FormSalesQuotationDTO
                {
                    RsqId = rsq.Id,
                    RequestCode = rsq.RequestCode,
                    Taxes = taxDtos,
                    Notes = noteDtos,
                    LotProducts = lotDtos,
                };

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = form
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Tạo form thất bại",
                };
            }
        }

        public async Task<ServiceResult<object>> CreateSalesQuotationAsync(CreateSalesQuotationDTO dto, string ssId)
        {
            try
            {
                var staffProfile = await ValidateSalesStaffStringId(ssId);

                var rsq = await _unitOfWork.RequestSalesQuotation.Query()
                    .Include(r => r.RequestSalesQuotationDetails)
                    .FirstOrDefaultAsync(r => r.Id == dto.RsqId);

                var rsqValidation = ValidateRequestSalesQuotation(rsq);
                if (rsqValidation != null)
                    return rsqValidation;

                var noteValidation = await ValidateNoteAsync(dto.NoteId);
                if (noteValidation != null)
                    return noteValidation;

                var lotsValidation = await ValidateLotsAsync(dto, rsq);
                if (lotsValidation != null)
                    return lotsValidation;

                var taxValidation = await ValidateTaxesAsync(dto.Details);
                if (taxValidation != null)
                    return taxValidation;

                if (dto.ExpiredDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Ngày hết hạn không hợp lệ"
                    };

                await _unitOfWork.BeginTransactionAsync();

                var salesQuotation = new Core.Domain.Entities.SalesQuotation
                {
                    RsqId = rsq.Id,
                    SsId = staffProfile.Id,
                    SqnId = dto.NoteId,
                    QuotationCode = GenerateQuotationCode(),
                    ExpiredDate = dto.ExpiredDate.Date.AddDays(1).AddTicks(-1),
                    Status = Core.Domain.Enums.SalesQuotationStatus.Draft,
                    SalesQuotaionDetails = dto.Details.Select(item => new SalesQuotaionDetails
                    {
                        LotId = item.LotId,
                        TaxId = item.TaxId,
                        ProductId = item.ProductId,
                        Note = item.Note,
                    }).ToList(),
                };

                await _unitOfWork.SalesQuotation.AddAsync(salesQuotation);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Tạo báo giá thất bại",
                };
            }
        }

        private static string GenerateQuotationCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"SQ-{datePart}-{randomPart}";
        }

        private static ServiceResult<object>? ValidateRequestSalesQuotation(Core.Domain.Entities.RequestSalesQuotation? rsq)
        {
            if (rsq == null)
                return new ServiceResult<object>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy yêu cầu báo giá"
                };

            if (rsq.Status == Core.Domain.Enums.RequestSalesQuotationStatus.Draft)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Không thể tạo báo giá cho yêu cầu báo giá chưa được gửi"
                };

            return null;
        }

        private async Task<ServiceResult<object>?> ValidateLotsAsync(CreateSalesQuotationDTO dto, Core.Domain.Entities.RequestSalesQuotation rsq)
        {
            if (dto.Details == null || dto.Details.Count == 0)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách chi tiết báo giá không được để trống"
                };

            var productIds = dto.Details
                .Select(d => d.ProductId)
                .Distinct()
                .ToList();

            // kiem tra productId ton tai
            var existingProductIds = await _unitOfWork.Product.Query()
                .Where(p => productIds.Contains(p.ProductID))
                .Select(p => p.ProductID)
                .ToListAsync();

            if (existingProductIds.Count != productIds.Count)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có sản phẩm không tồn tại trong hệ thống"
                };

            var validProductIds = rsq.RequestSalesQuotationDetails
                .Select(r => r.ProductId)
                .ToHashSet();

            var lotIds = dto.Details
                .Where(d => d.LotId.HasValue)
                .Select(d => d.LotId!.Value)
                .ToList();

            // kt productId co thuoc pham vi RSQ
            var invalidProducts = dto.Details
                .Where(d => !validProductIds.Contains(d.ProductId))
                .Select(d => d.ProductId)
                .Distinct()
                .ToList();

            if (invalidProducts.Any())
            {
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có sản phẩm không thuộc phạm vi yêu cầu báo giá"
                };
            }

            // check trung lotId
            if (lotIds.Count != lotIds.Distinct().Count())
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách lô hàng có lô trùng lặp"
                };

            var lots = await _unitOfWork.LotProduct.Query()
                .Include(l => l.Product)
                .AsNoTracking()
                .Where(l => lotIds.Contains(l.LotID))
                .ToListAsync();

            if (lots.Count != lotIds.Count)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có lô hàng không tồn tại trong hệ thống"
                };

            var invalidLots = lots
                .Where(l => l.ExpiredDate.Date <= DateTime.Today || l.LotQuantity <= 0)
                .ToList();

            if (invalidLots.Any())
            {
                var names = string.Join(", ", invalidLots.Select(l => l.Product.ProductName));
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = $"Các lô hàng không hợp lệ (hết hạn hoặc hết hàng): {names}"
                };
            }

            var outOfScopeLots = lots
                .Where(l => !validProductIds.Contains(l.ProductID))
                .ToList();

            if (outOfScopeLots.Any())
            {
                var names = string.Join(", ", outOfScopeLots.Select(l => l.Product.ProductName));
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = $"Các lô hàng sau không thuộc phạm vi yêu cầu báo giá: {names}"
                };
            }

            // kt lo co khop voi san pham khong
            var mismatchedLots = dto.Details
                .Where(d => d.LotId.HasValue)
                .Join(
                    lots,
                    d => d.LotId.Value,
                    l => l.LotID,
                    (d, l) => new { Detail = d, Lot = l }
                )
                .Where(x => x.Detail.ProductId != x.Lot.ProductID)
                .Select(x => new { x.Detail.ProductId, x.Lot.Product.ProductName })
                .ToList();

            if (mismatchedLots.Any())
            {
                var names = string.Join(", ", mismatchedLots.Select(m => m.ProductName));
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = $"Có lô hàng không khớp với sản phẩm tương ứng: {names}"
                };
            }

            var invalidTaxRelations = dto.Details
                .Where(d =>
                    (d.LotId == null && d.TaxId != null) || // Lot null nhưng Tax có
                    (d.LotId != null && d.TaxId == null))   // Lot có nhưng Tax null
                .ToList();

            if (invalidTaxRelations.Any())
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có chi tiết báo giá không hợp lệ: nếu chưa chọn lô thì không được chọn thuế, còn nếu đã chọn lô thì phải chọn thuế"
                };

            // check san pham co thuc su het hang
            var productsWithoutLot = dto.Details
                .Where(d => !d.LotId.HasValue)
                .Select(d => d.ProductId)
                .ToList();

            if (productsWithoutLot.Any())
            {
                if (productsWithoutLot.Count != productsWithoutLot.Distinct().Count())
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Có sản phẩm không chọn lô bị trùng lặp trong danh sách chi tiết"
                    };
                }

                var inStockProducts = await _unitOfWork.Product.Query()
                    .Where(p => productsWithoutLot.Contains(p.ProductID))
                    .Where(p => p.LotProducts.Any(lp => lp.LotQuantity > 0 && lp.ExpiredDate > DateTime.Today))
                    .ToListAsync();

                if (inStockProducts.Any())
                {
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Các sản phẩm chưa chọn lô nhưng vẫn còn hàng"
                    };
                }
            }

            return null;
        }


        private async Task<ServiceResult<object>?> ValidateTaxesAsync(List<SalesQuotationDetailsDTO> dto)
        {
            var taxIds = dto
                .Where(d => d.TaxId.HasValue)
                .Select(d => d.TaxId!.Value)
                .Distinct()
                .ToList();

            if (!taxIds.Any())
                return null;

            var taxPolicies = await _unitOfWork.TaxPolicy.Query()
                .AsNoTracking()
                .Where(t => taxIds.Contains(t.Id) && t.Status == true)
                .ToListAsync();

            if (taxPolicies.Count != taxIds.Count)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có chính sách thuế không hợp lệ hoặc đã bị vô hiệu"
                };

            return null;
        }

        public async Task<ServiceResult<object>> UpdateSalesQuotationAsync(UpdateSalesQuotationDTO dto, string ssId)
        {
            try
            {
                var staffProfile = await ValidateSalesStaffStringId(ssId);

                var salesQuotation = await _unitOfWork.SalesQuotation.Query()
                .Include(sq => sq.SalesQuotaionDetails)
                .FirstOrDefaultAsync(sq => sq.Id == dto.SqId);

                var sqValidation = ValidateSalesQuotation(salesQuotation);
                if (sqValidation != null)
                    return sqValidation;

                var ssValidate = ValidateSalesStaff(staffProfile, salesQuotation);
                if (ssValidate != null)
                    return ssValidate;

                var noteValidation = await ValidateNoteAsync(dto.SqnId);
                if (noteValidation != null)
                    return noteValidation;

                var lotValidation = await ValidateSQLotUpdate(salesQuotation, dto.Details);
                if (lotValidation != null)
                    return lotValidation;

                var taxValidation = await ValidateTaxesAsync(dto.Details);
                if (taxValidation != null)
                    return taxValidation;

                if (dto.ExpiredDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Ngày hết hạn không hợp lệ"
                    };

                await _unitOfWork.BeginTransactionAsync();

                salesQuotation.SqnId = dto.SqnId;

                salesQuotation.ExpiredDate = dto.ExpiredDate.Date.AddDays(1).AddTicks(-1);

                foreach (var detailDto in dto.Details)
                {
                    var record = salesQuotation.SalesQuotaionDetails
                        .FirstOrDefault(d => d.LotId == detailDto.LotId);

                    if (record != null)
                        record.TaxId = detailDto.TaxId;
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Cập nhật thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Cập nhật thất bại",
                };
            }
        }

        private static ServiceResult<object>? ValidateSalesQuotation(Core.Domain.Entities.SalesQuotation? sq)
        {
            if (sq == null)
                return new ServiceResult<object>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy báo giá"
                };

            if (sq.Status != Core.Domain.Enums.SalesQuotationStatus.Draft)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Không thể sửa báo giá đã được gửi"
                };

            return null;
        }

        private async Task<ServiceResult<object>?> ValidateSQLotUpdate(Core.Domain.Entities.SalesQuotation sq, List<SalesQuotationDetailsDTO> details)
        {
            var lotIds = details
                .Where(d => d.LotId.HasValue)
                .Select(d => d.LotId!.Value)
                .ToList();

            if (lotIds.Count != lotIds.Distinct().Count())
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách cập nhật có lô trùng lặp"
                };

            if (lotIds.Any())
            {
                var lots = await _unitOfWork.LotProduct.Query()
                .Where(lp => lotIds.Contains(lp.LotID))
                .ToListAsync();

                if (lots.Count != lotIds.Count)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Có lô hàng không tồn tại"
                    };

                var outOfScope = sq.SalesQuotaionDetails
                .Where(d => d.LotId.HasValue && lotIds.Contains(d.LotId.Value))
                .ToList();

                if (outOfScope.Count != lotIds.Count)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Danh sách lô hàng có lô không thuộc phạm vi báo giá"
                    };
            }

            return null;
        }

        public async Task<ServiceResult<object>> DeleteSalesQuotationAsync(int sqId, string ssId)
        {
            try
            {
                var staffProfile = await ValidateSalesStaffStringId(ssId);

                var salesQuotation = await _unitOfWork.SalesQuotation.Query()
                    .Include(sq => sq.SalesQuotaionDetails)
                    .FirstOrDefaultAsync(sq => sq.Id == sqId);

                var sqValidation = ValidateSalesQuotation(salesQuotation);
                if (sqValidation != null)
                    return sqValidation;

                var ssValidate = ValidateSalesStaff(staffProfile, salesQuotation);
                if (ssValidate != null)
                    return ssValidate;

                await _unitOfWork.BeginTransactionAsync();

                var details = salesQuotation.SalesQuotaionDetails;

                _unitOfWork.SalesQuotationDetails.RemoveRange(details);

                _unitOfWork.SalesQuotation.Remove(salesQuotation);

                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Đã xóa báo giá"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Xóa thất bại",
                };
            }
        }

        public async Task<ServiceResult<List<SalesQuotationDTO>>> SalesQuotationListAsync(string role, string ssId)
        {
            try
            {
                var staffProfile = await ValidateSalesStaffStringId(ssId);

                var query = _unitOfWork.SalesQuotation.Query()
                    .AsNoTracking()
                    .Include(sq => sq.RequestSalesQuotation)
                    .AsQueryable();

                if (role.Equals("CUSTOMER"))
                {
                    query = query.Where(s => s.Status != Core.Domain.Enums.SalesQuotationStatus.Draft);
                }
                else
                {
                    query = query.Where(s => s.SsId == staffProfile.Id);
                }

                var list = await query.ToListAsync();

                var result = _mapper.Map<List<SalesQuotationDTO>>(list);

                return new ServiceResult<List<SalesQuotationDTO>>
                {
                    StatusCode = 200,
                    Data = result,
                    Message = result.Count > 0 ? "" : "Không có báo giá nào"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<List<SalesQuotationDTO>>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                };
            }
        }

        private static ServiceResult<object>? ValidateSalesStaff(StaffProfile staffProfile, Core.Domain.Entities.SalesQuotation salesQuotation)
        {
            if (staffProfile.Id != salesQuotation.SsId)
                return new ServiceResult<object>
                {
                    StatusCode = 403,
                    Message = "Bạn không có quyền thao tác trên báo giá này"
                };

            return null;
        }

        private async Task<StaffProfile> ValidateSalesStaffStringId(string ssId)
        {
            if (!int.TryParse(ssId, out var intId))
                throw new Exception("Try parse sales staff id loi");

            var staffProfile = await _unitOfWork.StaffProfile.Query()
                .Include(sp => sp.SalesQuotations)
                .FirstOrDefaultAsync(sp => sp.Id == intId)
                ?? throw new Exception("Khong tim thay sales staff profile hoac khong ton tai");

            return staffProfile;
        }

        public async Task<ServiceResult<object>> SendSalesQuotationAsync(int sqId, string ssId)
        {
            try
            {
                var staffProfile = await ValidateSalesStaffStringId(ssId);

                var salesQuotation = await _unitOfWork.SalesQuotation.Query()
                    .Include(sq => sq.RequestSalesQuotation)
                        .ThenInclude(rsq => rsq.CustomerProfile)
                            .ThenInclude(cp => cp.User)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.TaxPolicy)
                    .Include(sq => sq.SalesQuotationNote)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.LotProduct)
                            .ThenInclude(lp => lp.Product)
                    .Include(sq => sq.StaffProfile)
                        .ThenInclude(sp => sp.User)
                    .FirstOrDefaultAsync(sq => sq.Id == sqId);

                var sqValidation = ValidateSalesQuotation(salesQuotation);
                if (sqValidation != null)
                    return sqValidation;

                var ssValidate = ValidateSalesStaff(staffProfile, salesQuotation);
                if (ssValidate != null)
                    return ssValidate;

                if (salesQuotation.ExpiredDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Ngày hết hạn không còn hợp lệ nữa"
                    };

                await _unitOfWork.BeginTransactionAsync();

                var rsq = salesQuotation.RequestSalesQuotation;

                var customer = rsq.CustomerProfile.User;

                var staff = salesQuotation.StaffProfile.User;

                rsq.Status = Core.Domain.Enums.RequestSalesQuotationStatus.Quoted;

                _unitOfWork.RequestSalesQuotation.Update(rsq);

                await _unitOfWork.CommitAsync();

                salesQuotation.QuotationDate = DateTime.Now;

                salesQuotation.Status = Core.Domain.Enums.SalesQuotationStatus.Sent;

                _unitOfWork.SalesQuotation.Update(salesQuotation);

                await _unitOfWork.CommitAsync();

                var html = QuotationTemplate.GenerateQuotationHtml(salesQuotation);

                var pdfBytes = _pdfService.GeneratePdfFromHtml(html);

                await SendSalesQuotationEmailAsync(pdfBytes, "Báo giá.pdf", customer.Email);

                await _notificationService.SendNotificationToCustomerAsync(
                    staff.Id,
                    customer.Id,
                    "Báo giá",
                    "Bạn nhận được 1 báo giá mới",
                    Core.Domain.Enums.NotificationType.Message);

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Gửi báo giá thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                await _unitOfWork.RollbackTransactionAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Gửi báo giá thất bại",
                };
            }
        }

        private async Task<ServiceResult<object>?> ValidateNoteAsync(int noteId)
        {
            var note = await _unitOfWork.SalesQuotationNote
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == noteId && v.IsActive == true);

            if (note == null)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Ghi chú báo giá không hợp lệ hoặc đã bị vô hiệu"
                };

            return null;
        }

        private async Task SendSalesQuotationEmailAsync(byte[] attachmentBytes, string attachmentName, string customerEmail)
        {
            var body = EmailBody.SALES_QUOTATION(customerEmail);
            await _emailService.SendMailWithPDFAsync(EmailSubject.SALES_QUOTATION, body, customerEmail, attachmentBytes, attachmentName);
        }
    }
}
