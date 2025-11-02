using System;
using System.Drawing;
using System.Globalization;
using System.Text;
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
using PMS.Application.DTOs.PRFQ;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
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

                while (!string.IsNullOrEmpty(worksheet.Cells[row, 1].Text))
                {
                    if (int.TryParse(worksheet.Cells[row, 1].Text?.Trim(), out int productId))
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
                        if (!int.TryParse(worksheet.Cells[row, 1].Text?.Trim(), out int productId))
                        {
                            row++;
                            continue;
                        }

                        var description = worksheet.Cells[row, 3].Text?.Trim();
                        var dvt = worksheet.Cells[row, 4].Text?.Trim();
                        var unitPriceText = worksheet.Cells[row, 5].Text?.Trim();

                        var expiredCell = worksheet.Cells[row, 6];
                        var expiredDateDisplay = expiredCell.Text?.Trim();

                        decimal.TryParse(unitPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitPrice);

                        var approvedQty = approvedOrderedQuantities.TryGetValue(productId, out var orderedQty) ? orderedQty : 0;
                        var info = productInfoDict.TryGetValue(productId, out var product) ? product : null;

                        var suggestedQuantity = 0;

                        if (info != null)
                        {
                            suggestedQuantity = Math.Max(0, info.MaxQuantity - (info.TotalCurrentQuantity + approvedQty));
                        }

                        products.Add(new PreviewProductDto
                        {
                            ProductID = productId,
                            ProductName = info?.ProductName ?? "Unknown",
                            Description = description,
                            DVT = dvt,
                            UnitPrice = unitPrice,
                            ExpiredDateDisplay = expiredDateDisplay,
                            CurrentQuantity = info?.TotalCurrentQuantity ?? 0,
                            MinQuantity = info?.MinQuantity ?? 0,
                            MaxQuantity = info?.MaxQuantity ?? 0,
                            SuggestedQuantity = suggestedQuantity
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

            // Cấu hình chung
            ws.Cells.Style.Font.Name = "Arial";
            ws.Cells.Style.Font.Size = 11;
            ws.Cells.Style.WrapText = false;
            ws.DefaultRowHeight = 18;

            // Tiêu đề
            ws.Cells[1, 1, 1, 8].Merge = true;
            ws.Cells[1, 1].Value = "YÊU CẦU BÁO GIÁ (REQUEST FOR QUOTATION)";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 18;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Row(1).Height = 30;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[1, 1].Style.Border.BorderAround(ExcelBorderStyle.Medium);

            // Logo
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            if (File.Exists(logoPath))
            {
                var picture = ws.Drawings.AddPicture("Logo", new FileInfo(logoPath));
                picture.SetPosition(0, 0, 7, 0);
                picture.SetSize(120, 50);
            }

            int currentRow = 3;

            // Header bên gửi / nhận
            ws.Cells[currentRow, 1, currentRow, 4].Merge = true;
            ws.Cells[currentRow, 1].Value = "BÊN GỬI / SENDER";
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            ws.Cells[currentRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 255));
            ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[currentRow, 1].Style.Border.BorderAround(ExcelBorderStyle.Medium);

            ws.Cells[currentRow, 5, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 5].Value = "BÊN NHẬN / RECEIVER";
            ws.Cells[currentRow, 5].Style.Font.Bold = true;
            ws.Cells[currentRow, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 5].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 255));
            ws.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[currentRow, 5].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            currentRow++;

            // GỬI
            ws.Cells[currentRow, 1].Value = "Người gửi:";
            ws.Cells[currentRow, 2].Value = fullPrfq.User?.UserName ?? "N/A";
            ws.Cells[currentRow, 3].Value = "Số PRFQID:";
            ws.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[currentRow, 4].Value = prfq.PRFQID;
            ws.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            // NHẬN
            ws.Cells[currentRow, 5].Value = "Tên NCC:";
            ws.Cells[currentRow, 6, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 6].Value = fullPrfq.Supplier?.Name ?? "—";
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Mã số thuế:";
            ws.Cells[currentRow, 2].Value = fullPrfq.TaxCode ?? "—";
            ws.Cells[currentRow, 3].Value = "SĐT:";
            ws.Cells[currentRow, 4].Value = fullPrfq.MyPhone ?? "—";

            ws.Cells[currentRow, 5].Value = "Email:";
            ws.Cells[currentRow, 6, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 6].Value = fullPrfq.Supplier?.Email ?? "—";
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Địa chỉ:";
            ws.Cells[currentRow, 2, currentRow, 4].Merge = true;
            ws.Cells[currentRow, 2].Value = fullPrfq.MyAddress ?? "—";

            ws.Cells[currentRow, 5].Value = "Địa chỉ:";
            ws.Cells[currentRow, 6, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 6].Value = fullPrfq.Supplier?.Address ?? "—";
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Ngày gửi:";
            ws.Cells[currentRow, 2].Value = fullPrfq.RequestDate.ToString("dd/MM/yyyy");

            ws.Cells[currentRow, 5].Value = "Liên lạc:";
            ws.Cells[currentRow, 6, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 6].Value = fullPrfq.Supplier?.PhoneNumber ?? "—";

            ws.Cells[3, 1, currentRow, 4].Style.Border.BorderAround(ExcelBorderStyle.Medium);
            ws.Cells[3, 5, currentRow, 8].Style.Border.BorderAround(ExcelBorderStyle.Medium);

            currentRow += 2;

            // Tiêu đề danh sách sản phẩm
            ws.Cells[currentRow, 1, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 1].Value = "DANH SÁCH SẢN PHẨM (PRODUCT LIST)";
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            ws.Cells[currentRow, 1].Style.Font.Size = 14;
            ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[currentRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            ws.Cells[currentRow, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            currentRow++;

            // Header bảng sản phẩm : sltt ko can  "Số lượng tối thiểu"
            string[] headers = { "Số thứ tự", "Tên sản phẩm", "Mô tả", "Đơn vị" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[currentRow, i + 1].Value = headers[i];
            }

            ws.Cells[currentRow, 1, currentRow, 8].Style.Font.Bold = true;
            ws.Cells[currentRow, 1, currentRow, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 1, currentRow, 8].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 153, 0));
            ws.Cells[currentRow, 1, currentRow, 8].Style.Font.Color.SetColor(Color.White);
            ws.Cells[currentRow, 1, currentRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            // Nội dung sản phẩm
            foreach (var prp in fullPrfq.PRPS)
            {
                ws.Cells[currentRow, 1].Value = prp.Product.ProductID;
                ws.Cells[currentRow, 2].Value = prp.Product.ProductName;
                ws.Cells[currentRow, 3].Value = prp.Product.ProductDescription;
                ws.Cells[currentRow, 4].Value = prp.Product.Unit;
                //ws.Cells[currentRow, 5].Value = prp.Product.MinQuantity;
                currentRow++;
            }

            // Kẻ bảng
            var tableRange = ws.Cells[11, 1, currentRow - 1, 8];
            tableRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            tableRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            currentRow += 2;

            // Ghi chú
            ws.Cells[currentRow, 1, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 1].Value = "GHI CHÚ (NOTES)";
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            ws.Cells[currentRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 255));
            ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            string[] notes = {
        "• Vui lòng phản hồi báo giá qua email hoặc hệ thống trong thời gian sớm nhất.",
        "• Báo giá cần ghi rõ điều kiện thanh toán và thời gian giao hàng.",
        "• Đảm bảo tính trung thực, rõ ràng trong báo giá."
    };

            foreach (var note in notes)
            {
                ws.Cells[currentRow, 1, currentRow, 8].Merge = true;
                ws.Cells[currentRow, 1].Value = note;
                ws.Cells[currentRow, 1].Style.Font.Italic = true;
                ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                currentRow++;
            }

            currentRow += 3;

            // Footer
            ws.Cells[currentRow, 1, currentRow, 8].Merge = true;
            ws.Cells[currentRow, 1].Value =
                "(Khởi tạo từ CÔNG TY TNHH DƯỢC PHẨM SỐ 17 – MST: 030203002865 – Hotline: 0398233047)";
            ws.Cells[currentRow, 1].Style.Font.Italic = true;
            ws.Cells[currentRow, 1].Style.Font.Size = 9;
            ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Tự động điều chỉnh độ rộng
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

            // Tiêu đề đơn hàng
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

            // Logo
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            if (File.Exists(logoPath))
            {
                var picture = ws.Drawings.AddPicture("Logo", new FileInfo(logoPath));
                picture.SetPosition(0, 0, 8, 0);
                picture.SetSize(120, 50);
            }

            // Thông tin PO
            ws.Cells[3, 1].Value = "Số đơn hàng (PO No):";
            ws.Cells[3, 2].Value = po.POID;
            ws.Cells[4, 1].Value = "Ngày lập đơn (Date):";
            ws.Cells[4, 2].Value = DateTime.Now.ToString("dd/MM/yyyy");
            ws.Cells[3, 1, 4, 1].Style.Font.Bold = true;

            // dữ liệu liên quan
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

            // Bên bán
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

            // Bên mua
            ws.Cells[6, 6, 6, 10].Merge = true;
            ws.Cells[6, 6].Value = "BÊN MUA HÀNG (BUYER)";
            ws.Cells[6, 6].Style.Font.Bold = true;
            ws.Cells[6, 6].Style.Font.Size = 13;
            ws.Cells[6, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[6, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[6, 6].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(230, 230, 250));
            ws.Cells[6, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            ws.Cells[7, 6].Value = "Người mua:";
            ws.Cells[7, 7].Value = user.UserName ?? "N/A";
            ws.Cells[8, 6].Value = "Tên đơn vị:";
            ws.Cells[8, 7].Value = "CÔNG TY CỔ PHẦN DƯỢC PHẨM SỐ 17";
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
                decimal tax = lineTotal * 0.1m;
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
                ws.Cells[row, 7].Value = "10%";
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
            ws.Cells[row + 2, 9].Value = "Tiền thuế (10%):";
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
        "• Các điều khoản khác được áp dụng theo hợp đồng đã ký giữa hai bên.",
        "• (Khởi tạo từ CÔNG TY CỔ PHẦN DƯỢC PHẨM SỐ 17 – MST: 030203002865 – Hotline: 0398233047)"
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

        public async Task<ServiceResult<int>> ConvertExcelToPurchaseOrderAsync(string userId, PurchaseOrderInputDto input, PurchasingOrderStatus purchasingOrderStatus)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var excelPath = await _cache.GetStringAsync(input.ExcelKey);
                if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
                    throw new Exception("Lấy key thất bại, Vui lòng upload lại báo giá.");

                using var package = new ExcelPackage(new FileInfo(excelPath));
                var worksheet = package.Workbook.Worksheets[0];
                var supplierName = worksheet.Cells[4, 6].Text?.Trim();
                if (!int.TryParse(worksheet.Cells[2, 2].Text?.Trim(), out int YC))
                    throw new Exception("Không thể đọc YC từ file Excel.");
                var SenderUser = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
                var supplier = _unitOfWork.Supplier.Query().FirstOrDefault(sp => sp.Name == supplierName);
                if (supplier == null)
                {
                    return new ServiceResult<int>
                    {
                        StatusCode = 200,
                        Message = "Tên nhà sản xuất bị trống"
                    };
                }
                // Đọc QID từ Excel 
                if (!int.TryParse(worksheet.Cells[4, 4].Text?.Trim(), out int qId))
                    throw new Exception("Không thể đọc QID từ file Excel.");
                DateTime qEDate;
                var qEDateCell = worksheet.Cells[7, 4];
                var rawQEDate = qEDateCell.Text?.Trim();

                if (qEDateCell.Value is double serialValue)
                {
                   
                    qEDate = DateTime.FromOADate(serialValue);
                }
                else if (DateTime.TryParseExact(rawQEDate,
                    new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d/M/yyyy", "M/d/yyyy" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
                {
                    qEDate = parsed;
                }
                else if (DateTime.TryParse(rawQEDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedLoose))
                {
                    qEDate = parsedLoose;
                }
                else
                {
                    throw new Exception($"Không thể đọc ngày hết hạn từ Excel: '{rawQEDate}'");
                }

                // Kiểm tra hạn
                if (DateTime.Now > qEDate)
                {
                    return new ServiceResult<int>
                    {
                        StatusCode = 200,
                        Message = "Quotation đã quá hạn. Vui lòng yêu cầu nhà cung cấp cập nhật báo giá mới."
                    };
                }

                DateTime qSDate;
                var qSDateCell = worksheet.Cells[7, 2];
                var rawQSDate = qSDateCell.Text?.Trim();

                if (qSDateCell.Value is double value)
                {
                    
                    qSDate = DateTime.FromOADate(value);
                }
                else if (DateTime.TryParseExact(rawQSDate,
                    new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d/M/yyyy", "M/d/yyyy" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
                {
                    qSDate = parsed;
                }
                else if (DateTime.TryParse(rawQSDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedLoose))
                {
                    qSDate = parsedLoose;
                }
                else
                {
                    throw new Exception($"Đọc ngày gửi thất bại. Giá trị không hợp lệ: '{rawQSDate}'");
                }

                // tạo quotation
                var Quotation = new Quotation
                {
                    QID = qId,
                    SupplierID = supplier.Id,
                    SendDate = qSDate,
                    Status = SupplierQuotationStatus.InDate,
                    QuotationExpiredDate = qEDate,
                    PRFQID = YC
                };

                // Tạo PO mới
                var po = new PurchasingOrder
                {
                    OrderDate = DateTime.Now,
                    QID = Quotation.QID,
                    UserId = userId,
                    Total = 0,
                    Status = PurchasingOrderStatus.sent,
                };
                await _unitOfWork.Quotation.AddAsync(Quotation);
                await _unitOfWork.PurchasingOrder.AddAsync(po);
                await _unitOfWork.CommitAsync();

                // Lấy dictionary sản phẩm từ DB
                var products = await _unitOfWork.Product.Query()
                    .ToDictionaryAsync(p => p.ProductID, p => p.ProductName);

                // Lọc danh sách sản phẩm được chọn
                var selectedProductQuantities = input.Details
                    .Where(d => d.Quantity > 0)
                    .ToDictionary(d => d.ProductID, d => d.Quantity);

                var details = new List<PurchasingOrderDetail>();
                var QuotationDetails = new List<QuotationDetail>();
                int row = 11;

                // Đọc từng dòng trong Excel
                while (!string.IsNullOrEmpty(worksheet.Cells[row, 1].Text))
                {
                    var productIdText = worksheet.Cells[row, 1].Text?.Trim();
                    if (!int.TryParse(productIdText, out int productId))
                    {
                        row++;
                        continue;
                    }

                    // bỏ qua sản phẩm không được chọn
                    if (!selectedProductQuantities.TryGetValue(productId, out int quantity))
                    {
                        row++;
                        continue;
                    }

                    var description = worksheet.Cells[row, 3].Text?.Trim();
                    var dvt = worksheet.Cells[row, 4].Text?.Trim();
                    var unitPriceText = worksheet.Cells[row, 5].Text?.Trim();
                    var expiredDateText = worksheet.Cells[row, 6].Text?.Trim();

                    if (!decimal.TryParse(unitPriceText, out decimal unitPrice))
                        unitPrice = 0;

                    DateTime expectedExpiredDate = DateTime.MinValue;

                    // Parse ExpiredDate (tối ưu hóa theo nhiều format)
                    if (!string.IsNullOrEmpty(expiredDateText) && expiredDateText != "Chưa có lô hàng")
                    {
                        if (double.TryParse(expiredDateText, NumberStyles.Any, CultureInfo.InvariantCulture, out double serialDate))
                        {
                            try
                            {
                                expectedExpiredDate = DateTime.FromOADate(serialDate);
                            }
                            catch (ArgumentException)
                            {

                                return new ServiceResult<int>
                                {
                                    StatusCode = 200,
                                    Message = $"Invalid serial date {serialDate} at row {row}"
                                };
                            }
                        }
                        else
                        {
                            string[] dateFormats = {
                            "dd/MM/yyyy", "MM/yyyy", "MMM-yyyy", "MMMM-yyyy",
                            "MMM-yy", "MMMM-yy", "MMM-dd", "dd-MMM", "dd-MMM-yyyy",
                            "MMM dd", "MMM dd yyyy", "MMM dd-yy", "MMM dd-yyyy"
        };

                            expiredDateText = expiredDateText.Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase).Trim();

                            bool parsedSuccess = false;

                            foreach (var format in dateFormats)
                            {
                                if (DateTime.TryParseExact(expiredDateText, format, new CultureInfo("en-US"), DateTimeStyles.None, out DateTime parsed))
                                {
                                    expectedExpiredDate = parsed;
                                    parsedSuccess = true;
                                    break;
                                }
                            }

                            if (!parsedSuccess && DateTime.TryParse(expiredDateText, new CultureInfo("en-US"), DateTimeStyles.None, out DateTime parsedLoose))
                            {
                                expectedExpiredDate = parsedLoose;
                                parsedSuccess = true;
                            }

                            if (!parsedSuccess)
                            {
                                return new ServiceResult<int>
                                {
                                    StatusCode = 400,
                                    Message = $"Không parse được ExpiredDate '{expiredDateText}' tại dòng {row}"
                                };
                            }
                        }
                    }

                    if (expectedExpiredDate == DateTime.MinValue)
                    {

                        return new ServiceResult<int>
                        {
                            StatusCode = 200,
                            Message = $"Lỗi khi biên dịch ExpiredDate tại dòng {row}: giá trị '{expiredDateText}' không hợp lệ hoặc không có lô hàng."
                        };
                    }

                    var total = quantity * unitPrice * 1.1m;
                    var productName = products.ContainsKey(productId) ? products[productId] : "Unknown";

                    details.Add(new PurchasingOrderDetail
                    {
                        POID = po.POID,
                        ProductName = productName,
                        Description = description,
                        DVT = dvt,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        UnitPriceTotal = total,
                        ExpiredDate = expectedExpiredDate,
                        ProductID = productId,
                    });

                    po.Total += total;

                    QuotationDetails.Add(new QuotationDetail
                    {
                        QID = qId,
                        ProductID = productId,
                        ProductName = productName,
                        ProductDescription = description ?? string.Empty,
                        ProductUnit = dvt ?? string.Empty,
                        UnitPrice = unitPrice,
                        ProductDate = expectedExpiredDate
                    });
                    row++;
                }

                ////
                if (details.Count == 0)
                {
                    return new ServiceResult<int>
                    {
                        StatusCode = 200,
                        Message = "Không có sản phẩm nào được chọn để đặt hàng."
                    };
                }
                _unitOfWork.PurchasingOrderDetail.AddRange(details);
                _unitOfWork.QuotationDetail.AddRange(QuotationDetails);
                await _unitOfWork.CommitAsync();

                //  Gửi mail PO
                var poExcelBytes = await GeneratePOExcelAsync(userId, po);
                var SupplierEmail = worksheet.Cells[5, 6].Text?.Trim();
                if (SupplierEmail == null)
                {
                    return new ServiceResult<int>
                    {
                        StatusCode = 200,
                        Message = "Kiểm tra lại email nhà cung cấp"
                    };
                }
                if (purchasingOrderStatus == PurchasingOrderStatus.sent)
                {
                    await _emailService.SendEmailWithAttachmentAsync(
                        SupplierEmail,
                        "đơn hàng",
                        GeneratePOEmailBody(po),
                        poExcelBytes,
                        $"PO_{po.POID}.xlsx"
                    );
                }
                await _notificationService.SendNotificationToRolesAsync(
                   userId,
                   ["ACCOUNTANT"],
                   "Yêu cầu nhập hàng",
                   $"Nhân viên {SenderUser.UserName} đã gửi mail đặt hàng đến NCC:{supplier.Name}",
                   Core.Domain.Enums.NotificationType.Reminder
                   );

                await _unitOfWork.CommitTransactionAsync();

                try { File.Delete(excelPath); } catch { }

                return new ServiceResult<int>
                {
                    StatusCode = 200,
                    Message = "Thành công."
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<int>
                {
                    StatusCode = 400,
                    Message = "Thất bại."
                };
            }
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
                        CreatedBy = p.User.UserName,
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
                    p.CreatedBy
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
    }
}
