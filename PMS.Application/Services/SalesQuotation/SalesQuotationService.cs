using AutoMapper;
using Castle.Core.Resource;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.Hub;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Data.UnitOfWork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PMS.Application.Services.SalesQuotation
{
    public class SalesQuotationService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SalesQuotationService> logger,
        IPdfService pdfService,
        IEmailService emailService,
        INotificationService notificationService,
        IHubContext<SalesQuotationHub> hubContext) : Service(unitOfWork, mapper), ISalesQuotationService
    {
        private readonly ILogger<SalesQuotationService> _logger = logger;
        private readonly IPdfService _pdfService = pdfService;
        private readonly IEmailService _emailService = emailService;
        private readonly INotificationService _notificationService = notificationService;
        private IHubContext<SalesQuotationHub> _hubContext = hubContext;

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
                                && lp.LotQuantity > 0
                                && lp.SalePrice > 0)
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
                        var groupedLots = lots.GroupBy(l => new
                        {
                            l.SalePrice,
                            l.InputPrice,
                            l.ExpiredDate,
                            l.SupplierID,
                            l.ProductID,
                        });

                        var selectedLots = new List<LotProduct>();

                        foreach(var group in groupedLots)
                        {
                            var selected = group
                                .OrderBy(l => l.WarehouselocationID)
                                .First();

                            selectedLots.Add(selected);
                        }

                        lotDtos.AddRange(_mapper.Map<List<LotDTO>>(selectedLots));
                    }
                    else
                    {
                        lotDtos.Add(new LotDTO
                        {
                            ProductID = detail.ProductId,
                            ProductName = detail.Product.ProductName,
                            Unit = detail.Product.Unit,
                            Note = "Không tìm thấy lô nào còn hàng hợp lệ"
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

                var listTax = dto.Details.Select(d => d.TaxId).ToList();

                var taxValidation = await ValidateTaxesAsync(listTax);
                if (taxValidation != null)
                    return taxValidation;

                if (dto.ExpiredDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Ngày hết hạn cho báo giá không được nhỏ hơn hôm nay"
                    };

                if(dto.ExpiredDate.Date > DateTime.Today.AddDays(30))
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Ngày hết hạn cho báo giá không được vượt quá 30 ngày"
                    };

                if (dto.DepositDueDays >= dto.ExpectedDeliveryDate)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thời hạn thanh toán cọc không được bằng hoặc vượt quá ngày giao hàng dự kiến"
                    };

                if (dto.ExpectedDeliveryDate > 365)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thời hạn giao hàng dự kiến không được quá 1 năm"
                    };

                var lotIdsInDto = dto.Details
                    .Where(d => d.LotId != null)
                    .Select(d => d.LotId!.Value)
                    .ToList();

                if (lotIdsInDto.Any())
                {
                    var lotEntities = await _unitOfWork.LotProduct.Query()
                        .Where(l => lotIdsInDto.Contains(l.LotID))
                        .ToListAsync();

                    var groupedLots = lotEntities
                        .GroupBy(l => new
                        {
                            l.SalePrice,
                            l.InputPrice,
                            l.ExpiredDate,
                            l.SupplierID,
                            l.ProductID
                        });

                    var selectedLots = new List<LotProduct>();

                    foreach(var group in groupedLots)
                    {
                        var selected = group.OrderBy(l => l.WarehouselocationID).First();
                        selectedLots.Add(selected);
                    }

                    dto.Details = dto.Details
                        .Where(d => selectedLots.Any(sl => sl.LotID == d.LotId))
                        .ToList();
                }

                await _unitOfWork.BeginTransactionAsync();

                var salesQuotation = new Core.Domain.Entities.SalesQuotation
                {
                    RsqId = rsq.Id,
                    SsId = staffProfile.Id,
                    SqnId = dto.NoteId,
                    QuotationCode = GenerateQuotationCode(),
                    QuotationDate = dto.Status == 1 ? DateTime.Now : null,
                    ExpiredDate = dto.ExpiredDate.Date.AddDays(1).AddTicks(-1),
                    Status = dto.Status == 1 ? Core.Domain.Enums.SalesQuotationStatus.Sent : Core.Domain.Enums.SalesQuotationStatus.Draft,
                    DepositPercent = dto.DepositPercent,
                    DepositDueDays = dto.DepositDueDays,
                    ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
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

                if(dto.Status == 1)
                {
                    var salesQuotationData = await _unitOfWork.SalesQuotation.Query()
                    .Include(sq => sq.RequestSalesQuotation)
                        .ThenInclude(rsq => rsq.CustomerProfile)
                            .ThenInclude(cp => cp.User)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.TaxPolicy)
                    .Include(sq => sq.SalesQuotationNote)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.LotProduct)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.Product)
                    .Include(sq => sq.StaffProfile)
                        .ThenInclude(sp => sp.User)
                    .FirstOrDefaultAsync(sq => sq.Id == salesQuotation.Id);

                    rsq.Status = Core.Domain.Enums.RequestSalesQuotationStatus.Quoted;

                    _unitOfWork.RequestSalesQuotation.Update(rsq);

                    await _unitOfWork.CommitAsync();

                    var customer = salesQuotation.RequestSalesQuotation.CustomerProfile.User;

                    var staff = salesQuotation.StaffProfile.User;

                    var html = QuotationTemplate.GenerateQuotationHtml(salesQuotation);

                    var pdfBytes = _pdfService.GeneratePdfFromHtml(html);

                    await SendSalesQuotationEmailAsync(pdfBytes, "Báo giá.pdf", customer.Email);

                    var listOldSalesQuotation = await _unitOfWork.SalesQuotation.Query()
                        .Where(s => s.RsqId == salesQuotation.RsqId && s.Id != salesQuotation.Id && s.Status == Core.Domain.Enums.SalesQuotationStatus.Sent)
                        .ToListAsync();

                    if (listOldSalesQuotation.Any())
                    {
                        foreach(var item in listOldSalesQuotation)
                        {
                            item.Status = Core.Domain.Enums.SalesQuotationStatus.Invalid;
                        }

                        _unitOfWork.SalesQuotation.UpdateRange(listOldSalesQuotation);

                        await _unitOfWork.CommitAsync();
                    }

                    await _notificationService.SendNotificationToCustomerAsync(
                    staff.Id,
                    customer.Id,
                    "Bạn nhận được 1 thông báo mới",
                    $"Bạn nhận được 1 báo giá mới: {salesQuotation.QuotationCode}",
                    Core.Domain.Enums.NotificationType.Message);
                }

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
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"SQ-{randomPart}";
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
            {
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách chi tiết báo giá không được để trống"
                };
            }

            var productIds = dto.Details
                .Select(d => d.ProductId)
                .Distinct()
                .ToList();

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

            //var invalidLots = lots
            //    .Where(l => l.ExpiredDate.Date <= DateTime.Today || l.LotQuantity <= 0)
            //    .ToList();

            //if (invalidLots.Any())
            //{
            //    var names = string.Join(", ", invalidLots.Select(l => l.Product.ProductName));
            //    return new ServiceResult<object>
            //    {
            //        StatusCode = 400,
            //        Message = $"Các lô hàng không hợp lệ (hết hạn hoặc hết hàng): {names}"
            //    };
            //}

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

                //var inStockProducts = await _unitOfWork.Product.Query()
                //    .Where(p => productsWithoutLot.Contains(p.ProductID))
                //    .Where(p => p.LotProducts.Any(lp => lp.LotQuantity > 0 && lp.ExpiredDate > DateTime.Today))
                //    .ToListAsync();

                //if (inStockProducts.Any())
                //{
                //    return new ServiceResult<object>
                //    {
                //        StatusCode = 400,
                //        Message = "Các sản phẩm chưa chọn lô nhưng vẫn còn hàng"
                //    };
                //}
            }

            return null;
        }


        private async Task<ServiceResult<object>?> ValidateTaxesAsync(List<int?> dto)
        {
            var taxIds = dto
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
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
                    .Include(sq => sq.RequestSalesQuotation)
                        .ThenInclude(rsq => rsq.RequestSalesQuotationDetails)
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

                var lotValidation = await ValidateLotsUpdateAsync(dto, salesQuotation.RequestSalesQuotation);
                if (lotValidation != null)
                    return lotValidation;

                var listTax = dto.Details.Select(d => d.TaxId).ToList();

                var taxValidation = await ValidateTaxesAsync(listTax);
                if (taxValidation != null)
                    return taxValidation;

                if (dto.ExpiredDate.Date < DateTime.Now.Date)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Ngày hết hạn cho báo giá không được nhỏ hơn hôm nay"
                    };

                if (dto.ExpiredDate.Date > DateTime.Today.AddDays(30))
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Ngày hết hạn cho báo giá không được vượt quá 30 ngày"
                    };

                if (dto.DepositDueDays >= dto.ExpectedDeliveryDate)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thời hạn thanh toán cọc không được bằng hoặc vượt quá ngày giao hàng dự kiến"
                    };

                if(dto.ExpectedDeliveryDate > 365)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Thời hạn giao hàng dự kiến không được quá 1 năm"
                    };

                var lotIdsInDto = dto.Details
                    .Where(d => d.LotId != null)
                    .Select(d => d.LotId!.Value)
                    .ToList();

                if (lotIdsInDto.Any())
                {
                    var lotEntities = await _unitOfWork.LotProduct.Query()
                        .Where(l => lotIdsInDto.Contains(l.LotID))
                        .ToListAsync();

                    var groupedLots = lotEntities
                        .GroupBy(l => new
                        {
                            l.SalePrice,
                            l.InputPrice,
                            l.ExpiredDate,
                            l.SupplierID,
                            l.ProductID
                        });

                    var selectedLots = new List<LotProduct>();

                    foreach (var group in groupedLots)
                    {
                        var selected = group.OrderBy(l => l.WarehouselocationID).First();
                        selectedLots.Add(selected);
                    }

                    dto.Details = dto.Details
                        .Where(d => selectedLots.Any(sl => sl.LotID == d.LotId))
                        .ToList();
                }

                await _unitOfWork.BeginTransactionAsync();

                salesQuotation.SqnId = dto.SqnId;

                salesQuotation.ExpiredDate = dto.ExpiredDate.Date.AddDays(1).AddTicks(-1);

                salesQuotation.DepositPercent = dto.DepositPercent;

                salesQuotation.DepositDueDays = dto.DepositDueDays;

                salesQuotation.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;

                var oldDetails = salesQuotation.SalesQuotaionDetails.ToList();

                var newDetails = dto.Details;

                var listNewDetails = new List<SalesQuotaionDetails>();

                foreach (var detailDto in newDetails)
                {
                    if (detailDto.SqdId != null)
                    {
                        var record = oldDetails.FirstOrDefault(x => x.Id == detailDto.SqdId);
                        if (record != null)
                        {
                            record.TaxId = detailDto.TaxId;
                            record.Note = detailDto.Note;
                            record.LotId = detailDto.LotId;
                            record.ProductId = detailDto.ProductId;
                        }
                    }
                    else
                    {
                        var newRecord = new SalesQuotaionDetails
                        {
                            SqId = salesQuotation.Id,
                            LotId = detailDto.LotId,
                            TaxId = detailDto.TaxId,
                            ProductId = detailDto.ProductId,
                            Note = detailDto.Note
                        };

                        listNewDetails.Add(newRecord);
                    }
                }

                if (listNewDetails.Count > 0)
                    await _unitOfWork.SalesQuotationDetails.AddRangeAsync(listNewDetails);

                var dtoIds = newDetails
                    .Where(d => d.SqdId != 0)
                    .Select(d => d.SqdId)
                    .ToList();

                var toDelete = oldDetails.Where(x => !dtoIds.Contains(x.Id)).ToList();

                if (toDelete.Count > 0)
                    _unitOfWork.SalesQuotationDetails.RemoveRange(toDelete);

                if(dto.Status == 1)
                {
                    var salesQuotationData = await _unitOfWork.SalesQuotation.Query()
                    .Include(sq => sq.RequestSalesQuotation)
                        .ThenInclude(rsq => rsq.CustomerProfile)
                            .ThenInclude(cp => cp.User)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.TaxPolicy)
                    .Include(sq => sq.SalesQuotationNote)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.LotProduct)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.Product)
                    .Include(sq => sq.StaffProfile)
                        .ThenInclude(sp => sp.User)
                    .FirstOrDefaultAsync(sq => sq.Id == salesQuotation.Id);

                    var rsq = salesQuotation.RequestSalesQuotation;

                    rsq.Status = Core.Domain.Enums.RequestSalesQuotationStatus.Quoted;

                    _unitOfWork.RequestSalesQuotation.Update(rsq);

                    salesQuotation.QuotationDate = DateTime.Now;

                    salesQuotation.Status = Core.Domain.Enums.SalesQuotationStatus.Sent;

                    _unitOfWork.SalesQuotation.Update(salesQuotation);

                    await _unitOfWork.CommitAsync();

                    var customer = salesQuotation.RequestSalesQuotation.CustomerProfile.User;

                    var staff = salesQuotation.StaffProfile.User;

                    var html = QuotationTemplate.GenerateQuotationHtml(salesQuotation);

                    var pdfBytes = _pdfService.GeneratePdfFromHtml(html);

                    await SendSalesQuotationEmailAsync(pdfBytes, "Báo giá.pdf", customer.Email);

                    var listOldSalesQuotation = await _unitOfWork.SalesQuotation.Query()
                        .Where(s => s.RsqId == salesQuotation.RsqId && s.Id != salesQuotation.Id && s.Status == Core.Domain.Enums.SalesQuotationStatus.Sent)
                        .ToListAsync();

                    if (listOldSalesQuotation.Any())
                    {
                        foreach (var item in listOldSalesQuotation)
                        {
                            item.Status = Core.Domain.Enums.SalesQuotationStatus.Invalid;
                        }

                        _unitOfWork.SalesQuotation.UpdateRange(listOldSalesQuotation);

                        await _unitOfWork.CommitAsync();
                    }

                    await _notificationService.SendNotificationToCustomerAsync(
                    staff.Id,
                    customer.Id,
                    "Bạn nhận được 1 thông báo mới",
                    $"Bạn nhận được 1 báo giá mới: {salesQuotation.QuotationCode}",
                    Core.Domain.Enums.NotificationType.Message);

                    await _unitOfWork.CommitTransactionAsync();

                    return new ServiceResult<object>
                    {
                        StatusCode = 200,
                        Message = "Gửi báo giá thành công"
                    };
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

        public async Task<ServiceResult<List<SalesQuotationDTO>>> SalesQuotationListAsync(string role, string? ssId, string? customerId)
        {
            try
            {
                var query = _unitOfWork.SalesQuotation.Query()
                    .AsNoTracking()
                    .Include(sq => sq.RequestSalesQuotation)
                    .AsQueryable();

                if (role.Equals("CUSTOMER"))
                {
                    if (!int.TryParse(customerId, out var intId))
                        throw new Exception("Try parse customer id loi");

                    query = query.Where(s => s.Status != Core.Domain.Enums.SalesQuotationStatus.Draft && s.RequestSalesQuotation.CustomerId == intId);
                }
                else
                {
                    if (ssId == null)
                        return new ServiceResult<List<SalesQuotationDTO>>
                        {
                            StatusCode = 401,
                        };
                    var staffProfile = await ValidateSalesStaffStringId(ssId);
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
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.Product)
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
                        Message = "Ngày hết hạn không được nhỏ hơn hôm nay"
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

                var listOldSalesQuotation = await _unitOfWork.SalesQuotation.Query()
                        .Where(s => s.RsqId == salesQuotation.RsqId && s.Id != salesQuotation.Id && s.Status == Core.Domain.Enums.SalesQuotationStatus.Sent)
                        .ToListAsync();

                if (listOldSalesQuotation.Any())
                {
                    foreach (var item in listOldSalesQuotation)
                    {
                        item.Status = Core.Domain.Enums.SalesQuotationStatus.Invalid;
                    }

                    _unitOfWork.SalesQuotation.UpdateRange(listOldSalesQuotation);

                    await _unitOfWork.CommitAsync();
                }

                await _notificationService.SendNotificationToCustomerAsync(
                    staff.Id,
                    customer.Id,
                    "Bạn nhận được 1 thông báo mới",
                    $"Bạn nhận được 1 báo giá mới: {salesQuotation.QuotationCode}",
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

        public async Task<ServiceResult<object>> AddSalesQuotationComment(AddSalesQuotationCommentDTO dto, string userId)
        {
            try
            {
                var user = await _unitOfWork.Users.Query()
                    .Include(u => u.CustomerProfile)
                    .Include(u => u.StaffProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 401,
                        Message = "Người dùng không tồn tại"
                    };

                var salesQuotation = await _unitOfWork.SalesQuotation.Query()
                    .Include(sq => sq.RequestSalesQuotation)
                        .ThenInclude(rsq => rsq.CustomerProfile)
                            .ThenInclude(cp => cp.User)
                    .Include(sq => sq.StaffProfile)
                        .ThenInclude(sp => sp.User)
                    .FirstOrDefaultAsync(sq => sq.Id == dto.SqId);

                if (salesQuotation == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy báo giá"
                    };

                if (salesQuotation.Status == Core.Domain.Enums.SalesQuotationStatus.Draft)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Báo giá chưa được gửi không thể bình luận"
                    };

                var role = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.CUSTOMER);

                if (role)
                {
                    var customer = user.CustomerProfile
                        ?? throw new Exception("Khach hang chua co profile");

                    if (salesQuotation.RequestSalesQuotation.CustomerId != customer.Id)
                        return new ServiceResult<object>
                        {
                            StatusCode = 403,
                            Message = "Bạn không có quyền bình luận vào báo giá này"
                        };
                }
                else
                {
                    var staff = user.StaffProfile
                        ?? throw new Exception("Nhan vien chua co profile");

                    if (salesQuotation.SsId != staff.Id)
                        return new ServiceResult<object>
                        {
                            StatusCode = 403,
                            Message = "Bạn không có quyền bình luận vào báo giá này"
                        };
                }

                var comment = new SalesQuotationComment
                {
                    SqId = dto.SqId,
                    UserId = user.Id,
                    Content = dto.Content?.Trim()
                };

                await _unitOfWork.SalesQuotationComment.AddAsync(comment);

                await _unitOfWork.CommitAsync();

                await _hubContext.Clients.Groups(dto.SqId.ToString())
                    .SendAsync("ReceiveSalesQuotationComment", new
                    {
                        SqId = dto.SqId,
                        UserId = user.FullName,
                        Content = dto.Content?.Trim(),
                    });

                await _notificationService.SendNotificationToCustomerAsync(
                    user.Id,
                    role == true ? salesQuotation.StaffProfile.User.Id : salesQuotation.RequestSalesQuotation.CustomerProfile.User.Id,
                    "Bạn có 1 bình luận mới",
                    $"Bạn có 1 bình luận mới trong báo giá {salesQuotation.QuotationCode}",
                    Core.Domain.Enums.NotificationType.Message);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Bình luận thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                };
            }
        }

        public async Task<ServiceResult<object>> SalesQuotaionDetailsAsync(int sqId, string userId)
        {
            try
            {
                var salesQuotation = await _unitOfWork.SalesQuotation.Query()
                    .AsNoTracking()
                    .Include(sq => sq.SalesQuotationComments)
                        .ThenInclude(c => c.User)
                    .Include(sq => sq.RequestSalesQuotation)
                        .ThenInclude(r => r.CustomerProfile)
                            .ThenInclude(c => c.User)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(sqd => sqd.LotProduct)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(d => d.TaxPolicy)
                    .Include(sq => sq.SalesQuotaionDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(sq => sq.Id == sqId);

                if (salesQuotation == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy báo giá"
                    };

                var user = await _unitOfWork.Users.Query()
                    .AsNoTracking()
                    .Include(u => u.CustomerProfile)
                    .Include(u => u.StaffProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 401,
                        Message = "Không tìm thấy người dùng"
                    };

                var role = await _unitOfWork.Users.UserManager.IsInRoleAsync(user, UserRoles.CUSTOMER);

                if (role)
                {
                    var customer = user.CustomerProfile
                        ?? throw new Exception("Khach hang chua co profile");

                    if (salesQuotation.Status == Core.Domain.Enums.SalesQuotationStatus.Draft)
                        return new ServiceResult<object>
                        {
                            StatusCode = 400,
                            Message = "Báo giá chưa được gửi không thể xem"
                        };

                    if (salesQuotation.RequestSalesQuotation.CustomerId != customer.Id)
                        return new ServiceResult<object>
                        {
                            StatusCode = 403,
                            Message = "Bạn không có quyền xem báo giá này"
                        };
                }
                else
                {
                    var staff = user.StaffProfile
                        ?? throw new Exception("Nhan vien chua co profile");

                    if (salesQuotation.SsId != staff.Id)
                        return new ServiceResult<object>
                        {
                            StatusCode = 403,
                            Message = "Bạn không có quyền xem báo giá này"
                        };
                }

                var details = new List<ViewSalesQuotationDetailsDTO>();

                decimal subTotal = 0;
                decimal taxTotal = 0;

                foreach (var item in salesQuotation.SalesQuotaionDetails)
                {
                    if (item.LotProduct != null && item.TaxPolicy != null)
                    {
                        decimal taxRate = item.TaxPolicy.Rate;

                        decimal salePrice = item.LotProduct.SalePrice;

                        decimal itemSubTotal = 1 * salePrice;
                        decimal itemTax = itemSubTotal * taxRate;
                        decimal itemTotal = itemSubTotal + itemTax;

                        subTotal += itemSubTotal;
                        taxTotal += itemTax;

                        var detail = new ViewSalesQuotationDetailsDTO
                        {
                            Id = item.Id,
                            ProductName = item.Product.ProductName,
                            Unit = item.Product.Unit,
                            TaxText = item.TaxPolicy.Name,
                            ExpiredDate = item.LotProduct.ExpiredDate.ToString("dd/MM/yyyy"),
                            minQuantity = 1,
                            SalesPrice = salePrice,
                            ItemTotal = itemTotal,
                            Note = item.Note
                        };

                        details.Add(detail);
                    }
                    else
                    {
                        var detail = new ViewSalesQuotationDetailsDTO
                        {
                            Id = item.Id,
                            ProductName = item.Product.ProductName,
                            Unit = item.Product.Unit,
                            Note = item.Note
                        };

                        details.Add(detail);
                    }
                }

                decimal grandTotal = subTotal + taxTotal;

                var validityTimeSpan = salesQuotation.QuotationDate.HasValue ? salesQuotation.ExpiredDate - salesQuotation.QuotationDate : null;

                string validityText;

                if (validityTimeSpan.HasValue)
                {
                    var ts = validityTimeSpan.Value;

                    if (ts.TotalDays >= 1)
                    {
                        int days = (int)Math.Floor(ts.TotalDays);
                        validityText = $"{days} ngày";
                    }
                    else if (ts.TotalHours >= 1)
                    {
                        int hours = (int)Math.Floor(ts.TotalHours);
                        validityText = $"{hours} giờ";
                    }
                    else
                    {
                        int minutes = (int)Math.Ceiling(ts.TotalMinutes); // làm tròn lên
                        validityText = $"{minutes} phút";
                    }
                }
                else
                {
                    validityText = "Chưa xác định";
                }

                var sqDTO = new ViewSalesQuotationDTO
                {
                    Id = salesQuotation.Id,
                    RequestCode = salesQuotation.RequestSalesQuotation.RequestCode,
                    QuotationCode = salesQuotation.QuotationCode,
                    QuotationDate = salesQuotation.QuotationDate,
                    ExpiredDate = salesQuotation.ExpiredDate,
                    Status = salesQuotation.Status,
                    Details = details,
                    Comments = _mapper.Map<List<SalesQuotationCommentDTO>>(salesQuotation.SalesQuotationComments?.ToList()),
                    subTotal = subTotal,
                    taxTotal = taxTotal,
                    grandTotal = grandTotal,
                    PharmacyName = "NHÀ THUỐC DƯỢC PHẨM SỐ 17",
                    Email = "minhtran2912003@gmail.com",
                    SenderAddress = "Kiot số 17, Phường Lê Thanh Nghị, TP Hải Phòng",
                    SenderPhone = "0398233047",
                    SenderName = salesQuotation.StaffProfile.User.FullName ?? "",
                    ReceiverName = salesQuotation.RequestSalesQuotation.CustomerProfile.User.FullName ?? "",
                    ReceiverPhone = salesQuotation.RequestSalesQuotation.CustomerProfile.User.PhoneNumber ?? "",
                    ReceiverMst = salesQuotation.RequestSalesQuotation.CustomerProfile.Mst,
                    ReceiverAddress = salesQuotation.RequestSalesQuotation.CustomerProfile.User.Address,
                    note = $@"Hiệu lực báo giá có giá trị {validityText} kể từ lúc báo giá.
Quá thời hạn trên, giá chào trong bản báo giá này có thể được điều chỉnh theo thực tế.
Tạm ứng {salesQuotation.DepositPercent.ToString("0.##")}% tiền cọc trong vòng {salesQuotation.DepositDueDays} ngày kể từ khi ký hợp đồng.
Hàng hóa dự kiến giao trong thời gian {salesQuotation.ExpectedDeliveryDate} ngày kể từ ngày ký kết hợp đồng và cọc.
Thanh toán bằng tiền mặt hoặc chuyển khoản vào tài khoản NGUYEN QUANG TRUNG - 4619300024210402 - Ngân hàng Timo."
                };

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = sqDTO
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");

                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi",
                };
            }
        }

        public async Task UpdateExpiredQuotationAsync()
        {
            try
            {
                var quotations = await _unitOfWork.SalesQuotation.Query()
                    .Where(sq => sq.Status == Core.Domain.Enums.SalesQuotationStatus.Sent && sq.ExpiredDate < DateTime.Now)
                    .ToListAsync();

                foreach (var q in quotations)
                    q.Status = Core.Domain.Enums.SalesQuotationStatus.Expired;

                if (quotations.Any())
                    await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
            }
        }

        private async Task<ServiceResult<object>?> ValidateLotsUpdateAsync(UpdateSalesQuotationDTO dto, Core.Domain.Entities.RequestSalesQuotation rsq)
        {
            if (dto.Details == null || dto.Details.Count == 0)
            {
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Danh sách chi tiết báo giá không được để trống"
                };
            }

            // --- VALIDATE 1: SqdId trung trong DTO ---
            var dtoIds = dto.Details
                .Where(d => d.SqdId.HasValue)
                .Select(d => d.SqdId!.Value)
                .ToList();

            if (dtoIds.Distinct().Count() != dtoIds.Count)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Có báo giá chi tiết bị trùng lặp"
                };

            // --- VALIDATE 2: SqdId khong ton tai trong DB ---
            var existingIds = await _unitOfWork.SalesQuotationDetails
                .Query()
                .Where(x => x.SqId == dto.SqId && dtoIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            var notFoundIds = dtoIds.Except(existingIds).ToList();

            if (notFoundIds.Any())
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message =
                      "Có chi tiết báo giá không tồn tại hoặc không thuộc báo giá hiện tại: " +
                      string.Join(", ", notFoundIds)
                };

            //var sqdIds = dto.Details.Select(d => d.SqdId).ToList();

            var productIds = dto.Details
                .Select(d => d.ProductId)
                .Distinct()
                .ToList();

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
                .ToList();

            var lotIds = dto.Details
                .Where(d => d.LotId.HasValue)
                .Select(d => d.LotId!.Value)
                .ToList();

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
            }
            return null;
        }
    }
}
