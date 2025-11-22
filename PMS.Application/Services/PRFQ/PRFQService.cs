using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.PRFQ;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Helper;
using PMS.Core.Domain.Identity;

using PMS.Data.UnitOfWork;

namespace PMS.API.Services.PRFQService
{
    public class PRFQService : Service, IPRFQService
    {
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PRFQService> _logger;

        public PRFQService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService, IDistributedCache cache, ILogger<PRFQService> logger, INotificationService notificationService)
            : base(unitOfWork, mapper)
        {

            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _cache = cache;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<int>> CreatePRFQAsync(string userId, int supplierId, string taxCode, string myPhone, string myAddress, List<int> productIds, PRFQStatus status)
        {
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<int>
                {
                    Message = "User không tồn tại, không có quyền tạo",
                    StatusCode = 404
                };
            }
            var supplier = await _unitOfWork.Supplier.Query()
                .FirstOrDefaultAsync(s => s.Id == supplierId && s.Status == SupplierStatus.Active);
            if (supplier == null)
            {
                return new ServiceResult<int>
                {
                    Message = "Supplier không tồn tại",
                    StatusCode = 404
                };
            }
            var products = await _unitOfWork.Product.Query()
                 .Where(p => productIds.Contains(p.ProductID))
                 .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return new ServiceResult<int>
                {
                    StatusCode = 400,
                    Message = "Một số sản phẩm không tồn tại"
                };
            }

            var inactiveProducts = products.Where(p => p.Status == false).ToList();
            if (inactiveProducts.Any())
            {
                return new ServiceResult<int>
                {
                    StatusCode = 400,
                    Message = "Một số sản phẩm không hoạt động"
                };
            }


            var prfq = new PurchasingRequestForQuotation
            {
                RequestDate = DateTime.Now,
                TaxCode = taxCode,
                MyPhone = myPhone,
                MyAddress = myAddress,
                SupplierID = supplierId,
                UserId = userId,
                Status = status,
            };

            await _unitOfWork.PurchasingRequestForQuotation.AddAsync(prfq);
            await _unitOfWork.CommitAsync();

            foreach (var productId in productIds)
            {
                var prp = new PurchasingRequestProduct
                {
                    PRFQID = prfq.PRFQID,
                    ProductID = productId
                };
                await _unitOfWork.PurchasingRequestProduct.AddAsync(prp);
            }
            await _unitOfWork.CommitAsync();

            // Generate Excel và gửi email
            var excelBytes = GenerateExcel(prfq);
            if (status == PRFQStatus.Sent)
            {
                await _emailService.SendEmailWithAttachmentAsync(supplier.Email, "Yêu cầu báo giá", "Kính gửi, đính kèm yêu cầu báo giá.", excelBytes, $"PRFQ_{prfq.PRFQID}.xlsx");
            }
            return new ServiceResult<int>
            {
                Data = prfq.PRFQID,
                Message = "Tạo yêu cầu báo giá thành công.",
                StatusCode = 200
            };
        }

        public async Task<ServiceResult<int>> ContinueEditPRFQ(int prfqId, ContinuePRFQDTO dto)
        {
            try
            {
                var currentPrfq = await _unitOfWork.PurchasingRequestForQuotation
                    .Query()
                    .Include(p => p.PRPS)
                    .FirstOrDefaultAsync(p => p.PRFQID == prfqId);

                if (currentPrfq == null)
                {
                    return new ServiceResult<int>
                    {
                        Data = 0,
                        Message = $"Không tìm thấy PRFQ với ID = {prfqId}.",
                        StatusCode = 404,
                        Success = false
                    };
                }

                if (currentPrfq.Status != PRFQStatus.Draft)
                {
                    return new ServiceResult<int>
                    {
                        Data = prfqId,
                        Message = $"PRFQ {prfqId} không thể chỉnh sửa vì trạng thái hiện tại là '{currentPrfq.Status}'.",
                        StatusCode = 400,
                        Success = false
                    };
                }


                currentPrfq.RequestDate = DateTime.Now;
                currentPrfq.Status = dto.PRFQStatus;
                _unitOfWork.PurchasingRequestForQuotation.Update(currentPrfq);


                var existingProducts = currentPrfq.PRPS.Select(x => x.ProductID).ToList();


                var toAdd = dto.ProductIds.Except(existingProducts).ToList();


                var toRemove = existingProducts.Except(dto.ProductIds).ToList();


                foreach (var productId in toAdd)
                {
                    await _unitOfWork.PurchasingRequestProduct.AddAsync(new PurchasingRequestProduct
                    {
                        PRFQID = currentPrfq.PRFQID,
                        ProductID = productId
                    });
                }

                if (toRemove.Any())
                {
                    var removeEntities = currentPrfq.PRPS
                        .Where(p => toRemove.Contains(p.ProductID))
                        .ToList();

                    _unitOfWork.PurchasingRequestProduct.RemoveRange(removeEntities);
                }

                await _unitOfWork.CommitAsync();

                var excelBytes = GenerateExcel(currentPrfq);
                if (currentPrfq.Status == PRFQStatus.Sent)
                {
                    await _emailService.SendEmailWithAttachmentAsync(currentPrfq.Supplier.Email, "Yêu cầu báo giá", "Kính gửi, đính kèm yêu cầu báo giá.", excelBytes, $"PRFQ_{currentPrfq.PRFQID}.xlsx");
                }
                return new ServiceResult<int>
                {
                    Data = currentPrfq.PRFQID,
                    Message = $"Thành công.",
                    StatusCode = 200,
                    Success = true
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ContinueEditPRFQ failed for PRFQID = {prfqId}");
                return new ServiceResult<int>
                {
                    Data = 0,
                    Message = "Có lỗi xảy ra khi tiếp tục chỉnh sửa PRFQ.",
                    StatusCode = 500,
                    Success = false
                };
            }
        }

        public async Task<ServiceResult<bool>> DeletePRFQAsync(int prfqId, string userId)
        {
            try
            {
                var prfq = await _unitOfWork.PurchasingRequestForQuotation
                    .Query()
                    .Include(p => p.PRPS)
                    .FirstOrDefaultAsync(p => p.PRFQID == prfqId);

                if (prfq == null)
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        StatusCode = 404,
                        Message = $"Không tìm thấy yêu cầu báo giá với ID: {prfqId}"
                    };
                }

                if (prfq.UserId != userId)
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        StatusCode = 403,
                        Message = "Bạn không có quyền xóa yêu cầu báo giá này."
                    };
                }

                if (prfq.Status != PRFQStatus.Draft)
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        StatusCode = 400,
                        Message = "Chỉ có thể xóa yêu cầu báo giá ở trạng thái 'Draft'."
                    };
                }


                if (prfq.PRPS != null && prfq.PRPS.Any())
                {
                    _unitOfWork.PurchasingRequestProduct.RemoveRange(prfq.PRPS);
                }


                _unitOfWork.PurchasingRequestForQuotation.Remove(prfq);

                await _unitOfWork.CommitAsync();

                return new ServiceResult<bool>
                {
                    Data = true,
                    StatusCode = 200,
                    Message = "Xóa yêu cầu báo giá thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa PRFQ ID: {prfqId}", prfqId);
                return new ServiceResult<bool>
                {
                    Data = false,
                    StatusCode = 500,
                    Message = "Đã xảy ra lỗi hệ thống khi xóa yêu cầu báo giá."
                };
            }
        }

        public async Task<PreviewExcelResponse> PreviewExcelProductsAsync(IFormFile file)
        {
            try
            {
                var products = new List<PreviewProductDto>();

                var tempDir = Path.Combine(Path.GetTempPath(), "po_excel");
                Directory.CreateDirectory(tempDir);

                var excelKey = $"excel_{Guid.NewGuid()}";
                var excelPath = Path.Combine(tempDir, $"{excelKey}.xlsx");

                using (var stream = new FileStream(excelPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                using var package = new ExcelPackage(new FileInfo(excelPath));
                var worksheet = package.Workbook.Worksheets[0];

                var productIds = new List<int>();
                int row = 11;

                while (!string.IsNullOrEmpty(worksheet.Cells[row, 2].Text))
                {
                    if (int.TryParse(worksheet.Cells[row, 2].Text?.Trim(), out int productId))
                        productIds.Add(productId);
                    row++;
                }

                var productInfoDict = await _unitOfWork.Product.Query()
                    .Where(p => productIds.Contains(p.ProductID))
                    .Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.MinQuantity,
                        p.MaxQuantity,
                        p.TotalCurrentQuantity
                    })
                    .ToDictionaryAsync(p => p.ProductID, p => p);

                // Lấy tổng số lượng đã được phê duyệt
                var approvedOrderedQuantities = await _unitOfWork.PurchasingOrderDetail.Query()
                    .Include(pod => pod.PurchasingOrder)
                    .Where(pod => productIds.Contains(pod.ProductID)
                        && pod.PurchasingOrder.Status == PurchasingOrderStatus.approved)
                    .GroupBy(pod => pod.ProductID)
                    .Select(g => new
                    {
                        ProductID = g.Key,
                        TotalApprovedOrderedQuantity = g.Sum(x => x.Quantity)
                    })
                    .ToDictionaryAsync(x => x.ProductID, x => x.TotalApprovedOrderedQuantity);


                // Đọc dữ liệu từng dòng trong Excel
                row = 11;
                while (!string.IsNullOrEmpty(worksheet.Cells[row, 1].Text))
                {
                    try
                    {
                        if (!int.TryParse(worksheet.Cells[row, 2].Text?.Trim(), out int productId))
                        {
                            row++;
                            continue;
                        }
                        if (!int.TryParse(worksheet.Cells[row, 1].Text?.Trim(), out int stt))
                        {
                            row++;
                            continue;
                        }

                        var description = worksheet.Cells[row, 4].Text?.Trim();
                        var dvt = worksheet.Cells[row, 5].Text?.Trim();
                        var unitPriceText = worksheet.Cells[row, 6].Text?.Trim();
                        var tax = worksheet.Cells[row, 7].Text?.Trim();
                        var expiredCell = worksheet.Cells[row, 8];
                        var expiredDateDisplay = expiredCell.Text?.Trim();

                        decimal.TryParse(unitPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitPrice);
                        decimal taxPerProduct = ParseTax(tax);
                        var approvedQty = approvedOrderedQuantities.TryGetValue(productId, out var orderedQty) ? orderedQty : 0;
                        var info = productInfoDict.TryGetValue(productId, out var product) ? product : null;

                        var suggestedQuantity = 0;

                        if (info != null)
                        {
                            suggestedQuantity = Math.Max(0, info.MaxQuantity - (info.TotalCurrentQuantity + approvedQty));
                        }

                        products.Add(new PreviewProductDto
                        {
                            STT = stt,
                            ProductID = productId,
                            ProductName = info?.ProductName ?? "Unknown",
                            Description = description,
                            DVT = dvt,
                            UnitPrice = unitPrice,
                            tax = taxPerProduct,
                            ExpiredDateDisplay = expiredDateDisplay,
                            CurrentQuantity = info?.TotalCurrentQuantity ?? 0,
                            MinQuantity = info?.MinQuantity ?? 0,
                            MaxQuantity = info?.MaxQuantity ?? 0,
                            SuggestedQuantity = suggestedQuantity,

                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing row {row}: {ex.Message}");
                    }

                    row++;
                }

                await _cache.SetStringAsync(
                    excelKey,
                    excelPath,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    });

                return new PreviewExcelResponse
                {
                    ExcelKey = excelKey,
                    Products = products
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PreviewExcelProductsAsync: {ex.Message}");
                throw new Exception("Đã xảy ra lỗi trong quá trình xem trước file Excel. Vui lòng kiểm tra lại file hoặc thử lại sau.", ex);
            }
        }

        private byte[] GenerateExcel(PurchasingRequestForQuotation prfq)
        {

            var fullPrfq = _unitOfWork.PurchasingRequestForQuotation
                .GetByIdAsync(prfq.PRFQID, q => q
                    .Include(p => p.Supplier)
                    .Include(p => p.User)
                    .Include(p => p.PRPS).ThenInclude(prp => prp.Product))
                .Result;

            if (fullPrfq == null)
                throw new Exception("PRFQ không tồn tại");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Yêu cầu báo giá");


            ws.Cells.Style.Font.Name = "Arial";
            ws.Cells.Style.Font.Size = 11;
            ws.DefaultRowHeight = 18;
            ws.Cells.Style.WrapText = false;


            ws.Cells[1, 1, 1, 8].Merge = true;
            ws.Cells[1, 1].Value = "YÊU CẦU BÁO GIÁ (REQUEST FOR QUOTATION)";
            ws.Cells[1, 1].Style.ApplyTitleStyle(18, Color.FromArgb(0, 102, 204));


            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            if (File.Exists(logoPath))
            {
                var picture = ws.Drawings.AddPicture("Logo", new FileInfo(logoPath));
                picture.SetPosition(0, 0, 7, 0);
                picture.SetSize(120, 50);
            }

            int row = 3;


            ws.Cells[row, 1, row, 4].Merge = true;
            ws.Cells[row, 1].Value = "BÊN GỬI / SENDER";
            ws.Cells[row, 1].Style.ApplyHeaderBox(Color.FromArgb(240, 240, 255));

            ws.Cells[row, 5, row, 8].Merge = true;
            ws.Cells[row, 5].Value = "BÊN NHẬN / RECEIVER";
            ws.Cells[row, 5].Style.ApplyHeaderBox(Color.FromArgb(240, 240, 255));

            row++;


            ws.Cells[row, 1].Value = "Bên gửi";
            ws.Cells[row, 2].Value = "CÔNG TY TNHH DƯỢC PHẨM BBPHARMACY";
            ws.Cells[row, 3].Value = "Số PRFQID:";
            ws.Cells[row, 4].Value = prfq.PRFQID;

            ws.Cells[row, 5].Value = "Tên NCC:";
            ws.Cells[row, 6, row, 8].Merge = true;
            ws.Cells[row, 6].Value = fullPrfq.Supplier?.Name ?? "—";
            row++;

            ws.Cells[row, 1].Value = "Mã số thuế:";
            ws.Cells[row, 2].Value = fullPrfq.TaxCode ?? "—";
            ws.Cells[row, 3].Value = "SĐT:";
            ws.Cells[row, 4].Value = fullPrfq.MyPhone ?? "—";

            ws.Cells[row, 5].Value = "Email:";
            ws.Cells[row, 6, row, 8].Merge = true;
            ws.Cells[row, 6].Value = fullPrfq.Supplier?.Email ?? "—";
            row++;

            ws.Cells[row, 1].Value = "Địa chỉ:";
            ws.Cells[row, 2, row, 4].Merge = true;
            ws.Cells[row, 2].Value = fullPrfq.MyAddress ?? "—";

            ws.Cells[row, 5].Value = "Địa chỉ:";
            ws.Cells[row, 6, row, 8].Merge = true;
            ws.Cells[row, 6].Value = fullPrfq.Supplier?.Address ?? "—";
            row++;

            ws.Cells[row, 1].Value = "Ngày gửi: Hải Phòng ngày,";
            ws.Cells[row, 2].Value = fullPrfq.RequestDate.ToString("dd/MM/yyyy");

            ws.Cells[row, 5].Value = "Liên lạc:";
            ws.Cells[row, 6, row, 8].Merge = true;
            ws.Cells[row, 6].Value = fullPrfq.Supplier?.PhoneNumber ?? "—";

            ws.Cells[3, 1, row, 4].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            ws.Cells[3, 5, row, 8].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            row += 2;


            ws.Cells[row, 1, row, 8].Merge = true;
            ws.Cells[row, 1].Value = "DANH SÁCH SẢN PHẨM (PRODUCT LIST)";
            ws.Cells[row, 1].Style.ApplyTitleSection(Color.LightGray);
            row++;


            string[] headers = { "Số thứ tự", "Mã số", "Tên sản phẩm", "Mô tả", "Đơn vị" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cells[row, i + 1].Value = headers[i];

            ws.Cells[row, 1, row, 8].Style.ApplyTableHeader(Color.FromArgb(0, 153, 0));
            row++;


            int index = 1;
            foreach (var prp in fullPrfq.PRPS)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = prp.Product.ProductID;
                ws.Cells[row, 3].Value = prp.Product.ProductName;
                ws.Cells[row, 4].Value = prp.Product.ProductDescription;
                ws.Cells[row, 5].Value = prp.Product.Unit;
                row++;
            }


            var tableRange = ws.Cells[11, 1, row - 1, 8];
            tableRange.Style.ApplyThinBorder();

            row += 2;

            //Ghi chú
            ws.Cells[row, 1, row, 8].Merge = true;
            ws.Cells[row, 1].Value = "GHI CHÚ (NOTES)";
            ws.Cells[row, 1].Style.ApplyHeaderBox(Color.FromArgb(240, 240, 255));
            row++;

            string[] notes = {
        "• Vui lòng phản hồi báo giá qua email hoặc hệ thống trong thời gian sớm nhất.",
        "• Báo giá cần ghi rõ điều kiện thanh toán và thời gian đáo hạn thanh toán.",
        "• Đảm bảo tính trung thực, rõ ràng trong báo giá.",
        "• Yêu cầu file phản hồi báo giá theo chuẩn Format đã thống nhất.",
        "• Mọi thắc mắc, phát sinh vui lòng liên lạc theo SĐT đã đính kèm.",
        "• BBPhamarcy xin cam kết, đảm bảo tính pháp lý của những mặt hàng được yêu cầu báo giá, thuộc loại được cấp phép lưu hành của BYT (Bộ Y Tế) trên lãnh thổ Việt Nam.",
        "• BBPharmacy với tư cách bên mua, cam kết chịu mọi trách nhiệm trước pháp luật, hiến pháp nước CHXHCN Việt Nam."
    };
            foreach (var note in notes)
            {
                ws.Cells[row, 1, row, 8].Merge = true;
                ws.Cells[row, 1].Value = note;
                ws.Cells[row, 1].Style.Font.Italic = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                row++;
            }

            //  Footer 
            row += 3;
            ws.Cells[row, 1, row, 8].Merge = true;
            ws.Cells[row, 1].Value = "(Khởi tạo từ CÔNG TY TNHH DƯỢC PHẨM SỐ 17 – MST: 030203002865 – Hotline: 0398233047)";
            ws.Cells[row, 1].Style.Font.Italic = true;
            ws.Cells[row, 1].Style.Font.Size = 9;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Điều chỉnh 
            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            for (int i = 1; i <= 8; i++)
                ws.Column(i).Width = Math.Min(ws.Column(i).Width, 100);
            ws.View.ZoomScale = 100;

            return package.GetAsByteArray();
        }


        private async Task<byte[]> GeneratePOExcelAsync(string userId, PurchasingOrder po)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Đơn hàng");


            ws.Cells.Style.Font.Name = "Arial";
            ws.Cells.Style.Font.Size = 11;
            ws.Cells.Style.WrapText = false;
            ws.DefaultRowHeight = 18;


            ws.Cells[1, 1, 1, 10].Merge = true;
            ws.Cells[1, 1].Value = "ĐƠN HÀNG (PURCHASE ORDER CONFIRMATION)";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 18;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[1, 1].Style.Border.BorderAround(ExcelBorderStyle.Medium);


            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "CTTNHHBBPHARMACY.png");
            if (File.Exists(logoPath))
            {
                var picture = ws.Drawings.AddPicture("Logo", new FileInfo(logoPath));
                picture.SetPosition(0, 0, 8, 0);
                picture.SetSize(120, 50);
            }


            ws.Cells[3, 1].Value = "Số đơn hàng (PO No):";
            ws.Cells[3, 2].Value = po.POID;
            ws.Cells[4, 1].Value = "Ngày lập đơn (Date):";
            ws.Cells[4, 2].Value = DateTime.Now.ToString("dd/MM/yyyy");
            ws.Cells[3, 1, 4, 1].Style.Font.Bold = true;


            var Quotation = await _unitOfWork.Quotation.Query()
                .FirstOrDefaultAsync(q => q.QID == po.QID);
            if (Quotation == null)
                throw new Exception("Lỗi khi tạo file excel");

            var supplier = await _unitOfWork.Supplier.Query()
                .FirstOrDefaultAsync(sp => sp.Id == Quotation.SupplierID);
            if (supplier == null)
                throw new Exception("Lỗi khi tạo file excel");

            var user = await _unitOfWork.Users.Query()
                .FirstOrDefaultAsync(sp => sp.Id == userId);
            if (user == null)
                throw new Exception("Lỗi khi tạo file excel");


            ws.Cells[6, 1, 6, 4].Merge = true;
            ws.Cells[6, 1].Value = "BÊN BÁN HÀNG (SELLER)";
            ws.Cells[6, 1].Style.Font.Bold = true;
            ws.Cells[6, 1].Style.Font.Size = 13;
            ws.Cells[6, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[6, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[6, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(230, 230, 250));
            ws.Cells[6, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            ws.Cells[7, 1].Value = "Tên đơn vị:";
            ws.Cells[7, 2].Value = supplier.Name ?? "N/A";
            ws.Cells[8, 1].Value = "Email:";
            ws.Cells[8, 2].Value = supplier.Email ?? "N/A";
            ws.Cells[9, 1].Value = "Địa chỉ:";
            ws.Cells[9, 2].Value = supplier.Address ?? "N/A";
            ws.Cells[10, 1].Value = "Liên lạc (Phone):";
            ws.Cells[10, 2].Value = supplier.PhoneNumber ?? "—";


            ws.Cells[6, 6, 6, 10].Merge = true;
            ws.Cells[6, 6].Value = "BÊN MUA HÀNG (BUYER)";
            ws.Cells[6, 6].Style.Font.Bold = true;
            ws.Cells[6, 6].Style.Font.Size = 13;
            ws.Cells[6, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[6, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[6, 6].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(230, 230, 250));
            ws.Cells[6, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            ws.Cells[7, 6].Value = "Người mua:";
            ws.Cells[7, 7].Value = user.FullName ?? "N/A";
            ws.Cells[8, 6].Value = "Tên đơn vị:";
            ws.Cells[8, 7].Value = "CÔNG TY TNHH DƯỢC PHẨM BBPHARMACY";
            ws.Cells[9, 6].Value = "Mã số thuế:";
            ws.Cells[9, 7].Value = "030203002865";
            ws.Cells[10, 6].Value = "Hình thức thanh toán:";
            ws.Cells[10, 7].Value = "CK/TM";

            ws.Cells[6, 1, 10, 4].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            ws.Cells[6, 6, 10, 10].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            ws.Cells[6, 1, 10, 1].Style.Font.Bold = true;
            ws.Cells[6, 6, 10, 6].Style.Font.Bold = true;

            // danh sách sản phẩm
            ws.Cells[12, 1, 12, 10].Merge = true;
            ws.Cells[12, 1].Value = "DANH SÁCH SẢN PHẨM (PRODUCT LIST)";
            ws.Cells[12, 1].Style.Font.Bold = true;
            ws.Cells[12, 1].Style.Font.Size = 14;
            ws.Cells[12, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[12, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[12, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            ws.Cells[12, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            string[] headers = {
        "STT", "Tên sản phẩm", "ĐVT", "Số lượng",
        "Đơn giá", "Thành tiền", "Thuế suất", "Tiền thuế", "Ghi chú", "Hạn dùng"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cells[13, i + 1].Value = headers[i];

            ws.Cells[13, 1, 13, 10].Style.Font.Bold = true;
            ws.Cells[13, 1, 13, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[13, 1, 13, 10].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 153, 0));
            ws.Cells[13, 1, 13, 10].Style.Font.Color.SetColor(Color.White);
            ws.Cells[13, 1, 13, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var details = await _unitOfWork.PurchasingOrderDetail.Query()
                .Where(d => d.POID == po.POID)
                .ToListAsync();

            int row = 14;
            int index = 1;
            decimal totalAmount = 0, totalTax = 0;

            foreach (var d in details)
            {
                decimal lineTotal = d.Quantity * d.UnitPrice;
                decimal tax = lineTotal * d.Tax;
                totalAmount += lineTotal;
                totalTax += tax;

                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = d.ProductName;
                ws.Cells[row, 3].Value = d.DVT;
                ws.Cells[row, 4].Value = d.Quantity;
                ws.Cells[row, 5].Value = d.UnitPrice;
                ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 6].Value = lineTotal;
                ws.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 7].Value = $"{d.Tax * 100}%";
                ws.Cells[row, 8].Value = tax;
                ws.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 9].Value = d.Description;
                ws.Cells[row, 10].Value = d.ExpiredDate.ToString("dd/MM/yyyy") ?? "N/A";
                row++;
            }

            var dataRange = ws.Cells[13, 1, row - 1, 10];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            ws.Cells[row + 1, 9].Value = "Tổng tiền chưa thuế:";
            ws.Cells[row + 1, 10].Value = totalAmount;
            ws.Cells[row + 2, 9].Value = "Tiền thuế:";
            ws.Cells[row + 2, 10].Value = totalTax;
            ws.Cells[row + 3, 9].Value = "Tổng cộng:";
            ws.Cells[row + 3, 10].Value = totalAmount + totalTax;
            ws.Cells[row + 3, 9, row + 3, 10].Style.Font.Bold = true;
            ws.Cells[row + 1, 10, row + 3, 10].Style.Numberformat.Format = "#,##0.00";

            row += 5;
            ws.Cells[row, 1, row, 10].Merge = true;
            ws.Cells[row, 1].Value = "GHI CHÚ (NOTES)";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 13;
            ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(230, 230, 250));
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            string[] notes = {
        "• Đơn hàng có hiệu lực theo thỏa thuận. Mọi thắc mắc xin liên hệ bộ phận kinh doanh để được hỗ trợ.",
        "• Các điều khoản khác được áp dụng theo hợp đồng đã ký giữa hai bên, đảm bảo phản hồi kịp thời qua SĐT đã đính kèm.",
        "• Với tư cách bên mua BBPHARMACY xin cam kết hoàn thành đầy đủ, và đúng hạn nghĩa vụ thanh toán như đã thống nhất.",
        "• BBPHARMCY xin cam kết tính chính xác, minh bạch về pháp lý cũng như đảm bảo về tổng tiền sau cùng.",
        "• BBPHARMCY xin cam kết chịu trách nhiệm trước pháp luật, hiến pháp nước CHXNCN Việt Nam trong quá trình kiện tụng (NẾU CÓ) ",
        "• FILE ĐƯỢC LƯU VĨNH VIỄN, PHỤC VỤ MỤC ĐÍCH KÊ BIÊN - THEO NĐ SỐ 123/2020/NĐ-CP BTC ",
        "• (Khởi tạo tự động từ CÔNG TY TNHH DƯỢC PHẨM BBPHARMACY – MST: 030203002865 – Hotline: 0398233047 - BBPHARMACY RẤT VUI ĐƯỢC HỢP TÁC VỚI QUÝ ĐỐI TÁC, VÌ THỊNH VƯỢNG - PHÁT TRIỀN)",

    };

            row++;
            foreach (var note in notes)
            {
                ws.Cells[row, 1, row, 10].Merge = true;
                ws.Cells[row, 1].Value = note;
                ws.Cells[row, 1].Style.Font.Italic = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Row(row).Height = 20;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        private string GeneratePOEmailBody(PurchasingOrder po)
        {
            if (po == null)
            {
                throw new ArgumentNullException(nameof(po), "Purchasing order cannot be null.");
            }
            var details = _unitOfWork.PurchasingOrderDetail
                .Query()
                .Where(d => d.POID == po.POID)
                .ToList();
            var body = new StringBuilder(1024);
            body.AppendLine("Kính gửi dhgfamily,");
            body.AppendLine();
            body.AppendLine("Thông tin đơn hàng:");
            body.AppendLine($"Mã PO: {po.POID}");
            body.AppendLine($"Tổng giá trị: {po.Total} VND");
            body.AppendLine();
            body.AppendLine("Chi tiết đơn hàng:");
            body.AppendLine(new string('-', 80));
            body.AppendLine($"{"PODID",-10} {"Tên sản phẩm",-20} {"ĐVT",-10} {"Số lượng",10} {"Đơn giá",15} {"Tổng",15} {"Mô tả",-20}");
            body.AppendLine(new string('-', 80));
            foreach (var detail in details)
            {
                body.AppendLine(string.Format(
                    "{0,-10} {1,-20} {2,-10} {3,10:N0} {4,15:VND} {5,15:VND} {6,-20}",
                    detail.PODID,
                    Truncate(detail.ProductName, 20),
                    detail.DVT,
                    detail.Quantity,
                    detail.UnitPrice,
                    detail.UnitPriceTotal,
                    Truncate(detail.Description, 20)));
            }
            body.AppendLine(new string('-', 80));
            body.AppendLine();
            body.AppendLine("Trân trọng,");
            body.AppendLine("BBpharmacy");
            body.AppendLine("---");
            body.AppendLine("Email này được tạo tự động, vui lòng không trả lời trực tiếp.");
            return body.ToString();
        }
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }
            return value.Substring(0, maxLength - 3) + "...";
        }

        public async Task<ServiceResult<object>> GetPRFQDetailAsync(int prfqId)
        {
            try
            {
                var prfq = await _unitOfWork.PurchasingRequestForQuotation
                    .Query()
                    .Include(p => p.Supplier)
                    .Include(p => p.User)
                    .Include(p => p.PRPS)
                        .ThenInclude(prp => prp.Product)
                    .FirstOrDefaultAsync(p => p.PRFQID == prfqId);

                if (prfq == null)
                {
                    return new ServiceResult<object>
                    {
                        Data = null,
                        StatusCode = 404,
                        Message = $"Không tìm thấy yêu cầu báo giá với ID: {prfqId}"
                    };
                }


                bool hasQuotation = await _unitOfWork.Quotation
                    .Query()
                    .AnyAsync(q => q.PRFQID == prfq.PRFQID);


                var displayStatus = hasQuotation ? PRFQStatus.Approved : prfq.Status;


                var detail = new
                {
                    prfq.PRFQID,
                    prfq.RequestDate,
                    Status = displayStatus,
                    prfq.TaxCode,
                    prfq.MyPhone,
                    prfq.MyAddress,
                    Supplier = new
                    {
                        prfq.Supplier.Id,
                        prfq.Supplier.Name,
                        prfq.Supplier.Email,
                        prfq.Supplier.PhoneNumber,
                        prfq.Supplier.Address
                    },
                    CreatedBy = new
                    {
                        prfq.User.Id,
                        prfq.User.UserName,
                        prfq.User.Email
                    },
                    Products = prfq.PRPS.Select(x => new
                    {
                        x.ProductID,
                        x.Product.ProductName,
                        x.Product.ProductDescription,
                        x.Product.Status,
                        x.Product.Unit
                    })
                };

                return new ServiceResult<object>
                {
                    Data = detail,
                    StatusCode = 200,
                    Message = "Lấy thông tin yêu cầu báo giá thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết PRFQ ID: {prfqId}", prfqId);
                return new ServiceResult<object>
                {
                    Data = null,
                    StatusCode = 500,
                    Message = "Đã xảy ra lỗi hệ thống khi lấy chi tiết yêu cầu báo giá."
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<object>>> GetAllPRFQAsync()
        {
            try
            {

                var prfqs = await _unitOfWork.PurchasingRequestForQuotation
                    .Query()
                    .Include(p => p.Supplier)
                    .Include(p => p.User)
                    .Select(p => new
                    {
                        p.PRFQID,
                        p.RequestDate,
                        p.Status,
                        p.TaxCode,
                        p.MyPhone,
                        p.MyAddress,
                        SupplierName = p.Supplier.Name,
                        SupplierEmail = p.Supplier.Email,
                        SupplierAddress = p.Supplier.Address,
                        SupplierPhone = p.Supplier.PhoneNumber,
                        CreatedBy = p.User.FullName,

                    })
                    .OrderByDescending(p => p.RequestDate)
                    .ToListAsync();

                if (prfqs.Count == 0)
                {
                    return new ServiceResult<IEnumerable<object>>
                    {
                        Data = null,
                        StatusCode = 200,
                        Message = "Hiện tại không tìm thấy bất kỳ yêu cầu báo giá nào"
                    };
                }


                var quotationPRFQIDs = await _unitOfWork.Quotation
                    .Query()
                    .Select(q => q.PRFQID)
                    .Distinct()
                    .ToListAsync();


                var result = prfqs.Select(p => new
                {
                    p.PRFQID,
                    p.RequestDate,
                    Status = quotationPRFQIDs.Contains(p.PRFQID)
                        ? PRFQStatus.Approved
                        : p.Status,
                    p.TaxCode,
                    p.MyPhone,
                    p.MyAddress,
                    p.SupplierName,
                    p.SupplierEmail,
                    p.CreatedBy,
                    p.SupplierPhone,
                    p.SupplierAddress,
                }).ToList();

                return new ServiceResult<IEnumerable<object>>
                {
                    Data = result,
                    StatusCode = 200,
                    Message = "Lấy danh sách yêu cầu báo giá thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách PRFQ.");
                return new ServiceResult<IEnumerable<object>>
                {
                    Data = null,
                    StatusCode = 500,
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách yêu cầu báo giá."
                };
            }
        }

        public async Task<byte[]> GenerateExcelAsync(int prfqId)
        {
            var fullPrfq = await _unitOfWork.PurchasingRequestForQuotation.Query()
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PRPS).ThenInclude(prp => prp.Product)
                .FirstOrDefaultAsync(p => p.PRFQID == prfqId);

            if (fullPrfq == null)
                return null;

            return GenerateExcel(fullPrfq);
        }

        public async Task<ServiceResult<object>> PreviewPRFQAsync(int id)
        {
            var prfq = await _unitOfWork.PurchasingRequestForQuotation.Query()
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PRPS)
                    .ThenInclude(prp => prp.Product)
                .FirstOrDefaultAsync(p => p.PRFQID == id);

            if (prfq == null)
            {
                return ServiceResult<object>.Fail($"Không tìm thấy PRFQ với ID {id}", 404);
            }

            var result = new
            {
                prfq.PRFQID,
                prfq.RequestDate,
                prfq.TaxCode,
                prfq.MyPhone,
                prfq.MyAddress,
                Supplier = new
                {
                    prfq.Supplier?.Name,
                    prfq.Supplier?.Email,
                    prfq.Supplier?.Address,
                    prfq.Supplier?.PhoneNumber,
                },
                User = new
                {
                    prfq.User?.UserName,
                    prfq.User?.Email
                },
                Products = prfq.PRPS.Select(x => new
                {
                    x.Product.ProductID,
                    x.Product.ProductName,
                    x.Product.ProductDescription,
                    x.Product.Unit
                })
            };

            return ServiceResult<object>.SuccessResult(result, "Lấy thông tin preview PRFQ thành công", 200);
        }

        public async Task<ServiceResult<bool>> UpdatePRFQStatusAsync(int prfqId, PRFQStatus newStatus)
        {
            var prfq = await _unitOfWork.PurchasingRequestForQuotation
                .Query()
                .FirstOrDefaultAsync(p => p.PRFQID == prfqId);

            if (prfq == null)
                return new ServiceResult<bool>
                {
                    Data = false,
                    Message = $"Không tìm thấy prfq với id {prfqId}",
                    StatusCode = 200,
                    Success = false,
                };

            if (prfq.Status == PRFQStatus.Approved && newStatus == PRFQStatus.Draft)
                throw new Exception("Không thể chuyển từ trạng thái Approved về Draft.");
            prfq.Status = newStatus;
            _unitOfWork.PurchasingRequestForQuotation.Update(prfq);
            await _unitOfWork.CommitAsync();

            return new ServiceResult<bool>
            {
                Data = true,
                Message = "Thanh cong",
                StatusCode = 200,
                Success = true,
            };
        }

        public async Task<ServiceResult<int>> ConvertExcelToPurchaseOrderAsync(string userId, PurchaseOrderInputDto input, PurchasingOrderStatus purchasingOrderStatus)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                var excelPath = await _cache.GetStringAsync(input.ExcelKey);
                if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
                    throw new Exception("Lấy key thất bại, vui lòng upload lại báo giá.");

                using var package = new ExcelPackage(new FileInfo(excelPath));
                var worksheet = package.Workbook.Worksheets[0];
                var excelData = ReadExcelData(worksheet);

                var senderUser = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
                var supplier = _unitOfWork.Supplier.Query().FirstOrDefault(sp => sp.Name == excelData.SupplierName);
                if (supplier == null)
                    return new ServiceResult<int> { StatusCode = 200, Message = "Tên nhà sản xuất bị trống" };


                var quotation = await GetOrCreateQuotationAsync(excelData, supplier.Id);
                if (quotation == null)
                    return new ServiceResult<int> { StatusCode = 200, Message = "Báo giá đã quá hạn hoặc không hợp lệ." };


                var po = await CreatePurchaseOrderAsync(userId, quotation.QID, input, worksheet, excelData.IsNewQuotation, excelData.PaymentDueDate);

                if (purchasingOrderStatus == PurchasingOrderStatus.sent)
                {
                    await SendEmailAndNotificationAsync(po, supplier, senderUser, worksheet, purchasingOrderStatus, userId);
                }
                await _unitOfWork.CommitTransactionAsync();
                try { File.Delete(excelPath); } catch { }

                return new ServiceResult<int> { StatusCode = 200, Message = "Thành công." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertExcelToPurchaseOrderAsync failed");
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<int> { StatusCode = 400, Message = "Thất bại." };
            }
        }

        private ExcelData ReadExcelData(ExcelWorksheet worksheet)
        {
            var supplierName = worksheet.Cells[4, 6].Text?.Trim();
            if (!int.TryParse(worksheet.Cells[2, 2].Text?.Trim(), out int prfqId))
                throw new Exception("Không thể đọc YC từ file Excel.");
            if (!int.TryParse(worksheet.Cells[2, 7].Text?.Trim(), out int PayDD))
                throw new Exception("Không thể đọc ngày đáo hạn từ file Excel.");
            if (!int.TryParse(worksheet.Cells[4, 4].Text?.Trim(), out int qId))
                throw new Exception("Không thể đọc QID từ file Excel.");

            var sendDate = PMS.Core.Domain.Helper.ExcelDateHelper.ReadDateFromCell(worksheet.Cells[7, 2], "Đọc ngày gửi thất bại.");
            var expiredDate = PMS.Core.Domain.Helper.ExcelDateHelper.ReadDateFromCell(worksheet.Cells[7, 4], "Không thể đọc ngày hết hạn từ Excel");

            return new ExcelData
            {
                SupplierName = supplierName,
                PRFQID = prfqId,
                QID = qId,
                SendDate = sendDate,
                ExpiredDate = expiredDate,
                PaymentDueDate = PayDD
            };
        }

        private async Task<Quotation?> GetOrCreateQuotationAsync(ExcelData data, int supplierId)
        {
            var existingQuotation = await _unitOfWork.Quotation.Query()
                .FirstOrDefaultAsync(q => q.QID == data.QID && q.SupplierID == supplierId);

            if (existingQuotation != null)
            {
                if (DateTime.Now > existingQuotation.QuotationExpiredDate)
                    return null;
                data.IsNewQuotation = false;
                return existingQuotation;
            }

            if (DateTime.Now > data.ExpiredDate)
                return null;

            var quotation = new Quotation
            {
                QID = data.QID,
                SupplierID = supplierId,
                SendDate = data.SendDate,
                Status = SupplierQuotationStatus.InDate,
                QuotationExpiredDate = data.ExpiredDate,
                PRFQID = data.PRFQID
            };

            await _unitOfWork.Quotation.AddAsync(quotation);
            await _unitOfWork.CommitAsync();

            data.IsNewQuotation = true;
            return quotation;
        }

        private async Task<PurchasingOrder> CreatePurchaseOrderAsync(
        string userId, int qId, PurchaseOrderInputDto input, ExcelWorksheet worksheet, bool isNewQuotation, int PaymentDueDate)
        {
            var po = new PurchasingOrder
            {
                OrderDate = DateTime.Now,
                QID = qId,
                UserId = userId,
                Total = 0,
                Status = PurchasingOrderStatus.sent,
                PaymentDueDate = PaymentDueDate
            };

            await _unitOfWork.PurchasingOrder.AddAsync(po);
            await _unitOfWork.CommitAsync();

            var products = await _unitOfWork.Product.Query()
                .ToDictionaryAsync(p => p.ProductID, p => p.ProductName);

            const int excelStartRow = 11;

            var poDetails = new List<PurchasingOrderDetail>();
            var quotationDetails = new List<QuotationDetail>();


            if (isNewQuotation)
            {
                int currentRow = excelStartRow;
                while (true)
                {
                    var productIdText = worksheet.Cells[currentRow, 2].Text?.Trim();
                    if (string.IsNullOrEmpty(productIdText))
                        break;

                    if (!int.TryParse(productIdText, out int productId))
                        throw new Exception($"Không thể đọc ProductID tại dòng {currentRow}.");

                    var description = worksheet.Cells[currentRow, 4].Text?.Trim();
                    var dvt = worksheet.Cells[currentRow, 5].Text?.Trim();
                    var unitPriceText = worksheet.Cells[currentRow, 6].Text?.Trim();
                    var tax = worksheet.Cells[currentRow, 7].Text?.Trim();
                    var expiredDateText = worksheet.Cells[currentRow, 8].Text?.Trim();


                    decimal.TryParse(unitPriceText, out decimal unitPrice);
                    decimal taxPerProduct = ParseTax(tax);
                    DateTime expiredDate = DateTime.MinValue;
                    try { expiredDate = PMS.Core.Domain.Helper.ExcelDateHelper.ParseDateFromString(expiredDateText, currentRow); } catch { }

                    var productName = products.ContainsKey(productId) ? products[productId] : "Unknown";

                    quotationDetails.Add(new QuotationDetail
                    {
                        QID = qId,
                        ProductID = productId,
                        ProductName = productName,
                        ProductDescription = description ?? string.Empty,
                        ProductUnit = (dvt ?? string.Empty).Length > 100
                            ? (dvt ?? string.Empty).Substring(0, 100)
                            : (dvt ?? string.Empty),
                        UnitPrice = unitPrice,
                        ProductDate = expiredDate,
                        Tax = taxPerProduct
                    });

                    currentRow++;
                }

                if (quotationDetails.Count > 0)
                    _unitOfWork.QuotationDetail.AddRange(quotationDetails);
            }


            ///
            foreach (var item in input.Details.Where(d => d.Quantity > 0))
            {
                int row = excelStartRow + item.STT - 1;

                var productIdText = worksheet.Cells[row, 2].Text?.Trim();
                if (!int.TryParse(productIdText, out int productId))
                    throw new Exception($"Không thể đọc ProductID tại dòng {row} (STT {item.STT}).");

                var description = worksheet.Cells[row, 4].Text?.Trim();
                var dvt = worksheet.Cells[row, 5].Text?.Trim();
                var unitPriceText = worksheet.Cells[row, 6].Text?.Trim();
                var tax = worksheet.Cells[row, 7].Text?.Trim();
                var expiredDateText = worksheet.Cells[row, 8].Text?.Trim();

                decimal.TryParse(unitPriceText, out decimal unitPrice);
                decimal taxPerProduct = ParseTax(tax);
                DateTime expiredDate = DateTime.MinValue;
                try { expiredDate = PMS.Core.Domain.Helper.ExcelDateHelper.ParseDateFromString(expiredDateText, row); } catch { }

                var productName = products.ContainsKey(productId) ? products[productId] : "Unknown";
                decimal taxlast = item.Quantity * unitPrice * taxPerProduct;
                decimal total = item.Quantity * unitPrice + taxlast;
                poDetails.Add(new PurchasingOrderDetail
                {
                    POID = po.POID,
                    ProductID = productId,
                    ProductName = productName,
                    Description = description,
                    DVT = dvt,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    UnitPriceTotal = total,
                    ExpiredDate = expiredDate,
                    Tax = taxPerProduct
                });

                po.Total += total;
            }

            if (poDetails.Count == 0)
                throw new Exception("Không có sản phẩm nào được chọn để đặt hàng.");

            _unitOfWork.PurchasingOrderDetail.AddRange(poDetails);

            await _unitOfWork.CommitAsync();

            return po;
        }


        private decimal ParseTax(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;


            input = input.Replace("%", "").Trim();


            if (!decimal.TryParse(input, out decimal value))
                return 0;


            if (value > 1)
                return value / 100m;


            return value;
        }


        private async Task SendEmailAndNotificationAsync(PurchasingOrder po, Supplier supplier, User senderUser, ExcelWorksheet worksheet, PurchasingOrderStatus status, string userId)

        {
            var supplierEmail = worksheet.Cells[5, 6].Text?.Trim();
            if (string.IsNullOrWhiteSpace(supplierEmail))
                throw new Exception("Kiểm tra lại email nhà cung cấp");

            var poExcelBytes = await GeneratePOExcelAsync(userId, po);

            if (status == PurchasingOrderStatus.sent)
            {
                await _emailService.SendEmailWithAttachmentAsync(
                    supplierEmail,
                    "Đơn hàng",
                    GeneratePOEmailBody(po),
                    poExcelBytes,
                    $"PO_{po.POID}.xlsx"
                );

                await _notificationService.SendNotificationToRolesAsync(
                userId,
                ["ACCOUNTANT"],
                "Yêu cầu nhập hàng",
                $"Nhân viên {senderUser.UserName} đã gửi mail đặt hàng đến NCC: {supplier.Name}",
                Core.Domain.Enums.NotificationType.Reminder);
            }
        }

        private async Task SendEmailAndNotificationAsync2(PurchasingOrder po, Supplier supplier, User senderUser, PurchasingOrderStatus status, string userId)

        {
            var poExcelBytes = await GeneratePOExcelAsync(userId, po);
            if (status == PurchasingOrderStatus.sent)
            {
                await _emailService.SendEmailWithAttachmentAsync(
                    supplier.Email,
                    "Đơn hàng",
                    GeneratePOEmailBody(po),
                    poExcelBytes,
                    $"PO_{po.POID}.xlsx"
                );

                await _notificationService.SendNotificationToRolesAsync(
                userId,
                ["ACCOUNTANT"],
                "Yêu cầu nhập hàng",
                $"Nhân viên {senderUser.UserName} đã gửi mail đặt hàng đến NCC: {supplier.Name}",
                Core.Domain.Enums.NotificationType.Reminder);
            }
        }


        public async Task<ServiceResult<int>> CreatePurchaseOrderByQIDAsync(string userId,
        PurchaseOrderByQuotaionInputDto input)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var quotation = await _unitOfWork.Quotation.Query()
                    .Include(q => q.QuotationDetails)
                    .FirstOrDefaultAsync(q => q.QID == input.QID);

                if (quotation == null)
                    return new ServiceResult<int> { StatusCode = 404, Message = "Không tìm thấy báo giá.", Data = 0, Success = false };

                if (DateTime.Now > quotation.QuotationExpiredDate)
                    return new ServiceResult<int> { StatusCode = 400, Message = "Báo giá đã hết hạn.", Data = 0, Success = false };

                var supplier = await _unitOfWork.Supplier.Query()
                    .FirstOrDefaultAsync(s => s.Id == quotation.SupplierID);

                if (supplier == null)
                    return new ServiceResult<int> { StatusCode = 400, Message = "Không tìm thấy nhà cung cấp.", Data = 0, Success = false };

                var senderUser = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);

                var po = new PurchasingOrder
                {
                    OrderDate = DateTime.Now,
                    QID = quotation.QID,
                    UserId = userId,
                    Total = 0,
                    Status = input.Status,
                };

                await _unitOfWork.PurchasingOrder.AddAsync(po);
                await _unitOfWork.CommitAsync();

                var poDetails = new List<PurchasingOrderDetail>();

                foreach (var item in input.Details)
                {
                    // Tìm chi tiết trong báo giá khớp ProductID và ExpiredDate đảm bảo cho chọn linh hoạt
                    var matchedDetail = quotation.QuotationDetails
                        .FirstOrDefault(qd => qd.ProductID == item.ProductID && qd.ProductDate.Date == item.Date.Date);

                    if (matchedDetail == null)
                        throw new Exception($"Sản phẩm {item.ProductID} với ngày {item.Date:yyyy-MM-dd} không tồn tại trong báo giá {quotation.QID}");
                    decimal taxlast = item.Quantity * matchedDetail.UnitPrice * matchedDetail.Tax;
                    decimal total = item.Quantity * matchedDetail.UnitPrice + taxlast;

                    poDetails.Add(new PurchasingOrderDetail
                    {
                        POID = po.POID,
                        ProductID = matchedDetail.ProductID,
                        ProductName = matchedDetail.ProductName,
                        Description = matchedDetail.ProductDescription,
                        DVT = matchedDetail.ProductUnit,
                        Quantity = item.Quantity,
                        UnitPrice = matchedDetail.UnitPrice,
                        UnitPriceTotal = total,
                        ExpiredDate = matchedDetail.ProductDate,
                        Tax = matchedDetail.Tax,
                    });

                    po.Total += total;
                }

                if (!poDetails.Any())
                    throw new Exception("Không có sản phẩm nào hợp lệ trong đơn hàng.");

                _unitOfWork.PurchasingOrderDetail.AddRange(poDetails);
                await _unitOfWork.CommitAsync();

                if (input.Status == PurchasingOrderStatus.sent)
                {
                    await SendEmailAndNotificationAsync2(po, supplier, senderUser, input.Status, userId);
                }

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<int>
                {
                    StatusCode = 200,
                    Data = po.POID,
                    Message = "Tạo đơn hàng thành công.",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreatePurchaseOrderByQIDAsync failed");
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<int> { StatusCode = 400, Message = "Thất bại khi tạo đơn hàng." };
            }
        }


        public async Task<ServiceResult<bool>> CountinueEditPurchasingOrderAsync(int poid, string userid, PurchaseOrderByQuotaionInputDto input)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                var po = await _unitOfWork.PurchasingOrder.Query()
                    .Include(p => p.PurchasingOrderDetails)
                    .FirstOrDefaultAsync(p => p.POID == poid);

                if (po == null)
                    return new ServiceResult<bool> { StatusCode = 404, Data = false, Message = $"Không tìm thấy order với id:{poid}", Success = false };

                if (po.Status != PurchasingOrderStatus.draft)
                    return new ServiceResult<bool> { StatusCode = 400, Data = false, Message = "Không thể edit với PO không ở trạng thái nháp", Success = false };


                var exQ = await _unitOfWork.Quotation.Query()
                    .Include(q => q.QuotationDetails)
                    .FirstOrDefaultAsync(q => q.QID == po.QID);

                if (exQ == null)
                    return new ServiceResult<bool> { StatusCode = 404, Data = false, Message = $"Không tìm thấy báo giá với QID:{po.QID}", Success = false };

                if (DateTime.Now > exQ.QuotationExpiredDate)
                    return new ServiceResult<bool> { StatusCode = 400, Data = false, Message = "Báo giá đã hết hạn.", Success = false };


                var supplier = await _unitOfWork.Supplier.Query()
                    .FirstOrDefaultAsync(s => s.Id == exQ.SupplierID);
                if (supplier == null)
                    return new ServiceResult<bool> { StatusCode = 400, Data = false, Message = "Không tìm thấy nhà cung cấp.", Success = false };

                var senderUser = await _unitOfWork.Users.UserManager.FindByIdAsync(userid);


                if (po.PurchasingOrderDetails.Any())
                {
                    _unitOfWork.PurchasingOrderDetail.RemoveRange(po.PurchasingOrderDetails);
                    po.Total = 0;
                }

                // 5. Tạo chi tiết mới
                var poDetails = new List<PurchasingOrderDetail>();
                foreach (var item in input.Details)
                {
                    var matchedDetail = exQ.QuotationDetails
                        .FirstOrDefault(qd => qd.ProductID == item.ProductID && qd.ProductDate.Date == item.Date.Date);

                    if (matchedDetail == null)
                        throw new Exception($"Sản phẩm {item.ProductID} với ngày {item.Date:yyyy-MM-dd} không tồn tại trong báo giá {exQ.QID}");

                    decimal total = item.Quantity * matchedDetail.UnitPrice * 1.1m;

                    poDetails.Add(new PurchasingOrderDetail
                    {
                        POID = po.POID,
                        ProductID = matchedDetail.ProductID,
                        ProductName = matchedDetail.ProductName,
                        Description = matchedDetail.ProductDescription,
                        DVT = matchedDetail.ProductUnit,
                        Quantity = item.Quantity,
                        UnitPrice = matchedDetail.UnitPrice,
                        UnitPriceTotal = total,
                        ExpiredDate = matchedDetail.ProductDate
                    });

                    po.Total += total;
                }

                if (!poDetails.Any())
                    throw new Exception("Không có sản phẩm hợp lệ trong đơn hàng.");

                _unitOfWork.PurchasingOrderDetail.AddRange(poDetails);

                po.Status = input.Status;

                await _unitOfWork.CommitAsync();


                if (po.Status == PurchasingOrderStatus.sent)
                {
                    await SendEmailAndNotificationAsync2(po, supplier, senderUser, po.Status, userid);
                }

                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<bool>
                {
                    StatusCode = 200,
                    Data = true,
                    Message = "Cập nhật PO thành công.",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CountinueEditPurchasingOrderAsync failed");
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<bool> { StatusCode = 400, Message = "Thất bại khi cập nhật đơn hàng.", Success = false };
            }
        }


        public async Task<ServiceResult<IEnumerable<PreviewProductDto>>> PreviewExcelProductsByExcitedQuotationAsync(int QID)
        {
            try
            {
                var products = new List<PreviewProductDto>();

                var exQ = await _unitOfWork.Quotation.Query()
                    .Include(q => q.QuotationDetails)
                    .FirstOrDefaultAsync(q => q.QID == QID);

                if (exQ == null)
                    return new ServiceResult<IEnumerable<PreviewProductDto>> { Success = false, Data = null, Message = "Không tồn tại báo giá", StatusCode = 404 };

                if (DateTime.Now > exQ.QuotationExpiredDate)
                    return new ServiceResult<IEnumerable<PreviewProductDto>> { Success = false, Data = null, Message = "báo giá hết hạn", StatusCode = 404 };

                var supplier = await _unitOfWork.Supplier.Query()
                    .FirstOrDefaultAsync(s => s.Id == exQ.SupplierID);

                if (supplier == null)
                    return new ServiceResult<IEnumerable<PreviewProductDto>> { Success = false, Data = null, Message = "Không tồn NCC", StatusCode = 404 };

                // Danh sách sản phẩm trong báo giá
                var quotationProducts = exQ.QuotationDetails
                    .Select(q => new
                    {
                        q.ProductID,
                        q.ProductName,
                        q.ProductDescription,
                        q.ProductUnit,
                        q.UnitPrice,
                        q.ProductDate,
                        q.Tax
                    }).ToList();

                var productIds = quotationProducts.Select(x => x.ProductID).ToList();

                // Lấy thông tin sản phẩm trong hệ thống
                var productInfoDict = await _unitOfWork.Product.Query()
                    .Where(p => productIds.Contains(p.ProductID))
                    .Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.MinQuantity,
                        p.MaxQuantity,
                        p.TotalCurrentQuantity
                    })
                    .ToDictionaryAsync(x => x.ProductID, x => x);

                // Lấy tổng số lượng đã được phê duyệt
                var approvedOrderedQuantities = await _unitOfWork.PurchasingOrderDetail.Query()
                    .Include(pod => pod.PurchasingOrder)
                    .Where(pod => productIds.Contains(pod.ProductID)
                        && pod.PurchasingOrder.Status == PurchasingOrderStatus.approved)
                    .GroupBy(pod => pod.ProductID)
                    .Select(g => new
                    {
                        ProductID = g.Key,
                        TotalApprovedOrderedQuantity = g.Sum(x => x.Quantity)
                    })
                    .ToDictionaryAsync(x => x.ProductID, x => x.TotalApprovedOrderedQuantity);
                int stt = 1;
                foreach (var qProd in quotationProducts)
                {
                    approvedOrderedQuantities.TryGetValue(qProd.ProductID, out int approvedQty);
                    productInfoDict.TryGetValue(qProd.ProductID, out var info);

                    int suggestedQty = 0;

                    if (info != null)
                    {
                        suggestedQty = Math.Max(
                            0,
                            info.MaxQuantity - (info.TotalCurrentQuantity + approvedQty)
                        );
                    }
                    products.Add(new PreviewProductDto
                    {
                        STT = stt++,
                        ProductID = qProd.ProductID,
                        ProductName = qProd.ProductName,
                        Description = qProd.ProductDescription,
                        DVT = qProd.ProductUnit,
                        UnitPrice = qProd.UnitPrice,
                        ExpiredDateDisplay = qProd.ProductDate.ToString("dd/MM/yyyy"),
                        tax = qProd.Tax,
                        CurrentQuantity = info?.TotalCurrentQuantity ?? 0,
                        MinQuantity = info?.MinQuantity ?? 0,
                        MaxQuantity = info?.MaxQuantity ?? 0,
                        SuggestedQuantity = suggestedQty
                    });
                }
                return new ServiceResult<IEnumerable<PreviewProductDto>>
                {
                    Data = products,
                    StatusCode = 200,
                    Message = "Thành công",
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PreviewExcelProductsByExcitedQuotationAsync: {ex.Message}");
                throw new Exception("Đã xảy ra lỗi trong quá trình xem trước dữ liệu báo giá.", ex);
            }
        }
    }
}
