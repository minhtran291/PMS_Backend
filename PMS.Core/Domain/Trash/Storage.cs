using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Trash
{
    internal class Storage
    {
        //public async Task<ServiceResult<int>> ConvertExcelToPurchaseOrderAsync(string userId, PurchaseOrderInputDto input, PurchasingOrderStatus purchasingOrderStatus)
        //{
        //    await _unitOfWork.BeginTransactionAsync();
        //    try
        //    {
        //        var excelPath = await _cache.GetStringAsync(input.ExcelKey);
        //        if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
        //            throw new Exception("Lấy key thất bại, vui lòng upload lại báo giá.");

        //        using var package = new ExcelPackage(new FileInfo(excelPath));
        //        var worksheet = package.Workbook.Worksheets[0];

        //        var supplierName = worksheet.Cells[4, 6].Text?.Trim();
        //        if (!int.TryParse(worksheet.Cells[2, 2].Text?.Trim(), out int YC))
        //            throw new Exception("Không thể đọc YC từ file Excel.");

        //        var senderUser = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
        //        var supplier = _unitOfWork.Supplier.Query().FirstOrDefault(sp => sp.Name == supplierName);
        //        if (supplier == null)
        //            return new ServiceResult<int> { StatusCode = 200, Message = "Tên nhà sản xuất bị trống" };

        //        if (!int.TryParse(worksheet.Cells[4, 4].Text?.Trim(), out int qId))
        //            throw new Exception("Không thể đọc QID từ file Excel.");

        //        // --- Hàm đọc ngày ---
        //        DateTime ReadDateFromCell(ExcelRange cell, string fieldName)
        //        {
        //            if (cell == null) throw new Exception($"{fieldName} - ô không tồn tại.");
        //            var val = cell.Value;
        //            var text = cell.Text?.Trim() ?? string.Empty;
        //            if (val == null || string.IsNullOrWhiteSpace(text))
        //                throw new Exception($"{fieldName} bị trống.");

        //            if (val is double d)
        //                return DateTime.FromOADate(d);

        //            if (val is DateTime dt)
        //                return dt;

        //            var formats = new[]
        //            {
        //        "dd/MM/yyyy","d/M/yyyy","d-M-yyyy","dd-MM-yyyy",
        //        "MM/dd/yyyy","M/d/yyyy","yyyy-MM-dd","yyyy/MM/dd",
        //        "MMM yyyy","MMMM yyyy","dd MMM yyyy","dd-MMM-yyyy"
        //    };

        //            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedExact))
        //                return parsedExact;

        //            if (DateTime.TryParse(text, new CultureInfo("vi-VN"), DateTimeStyles.None, out var parsedVi))
        //                return parsedVi;

        //            if (DateTime.TryParse(text, new CultureInfo("en-US"), DateTimeStyles.None, out var parsedEn))
        //                return parsedEn;

        //            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedLoose))
        //                return parsedLoose;

        //            throw new Exception($"{fieldName} không thể parse được: '{text}'");
        //        }

        //        DateTime ParseDateFromString(string rawText, int row)
        //        {
        //            var text = rawText?.Trim() ?? string.Empty;
        //            if (string.IsNullOrEmpty(text) || text.Equals("Chưa có lô hàng", StringComparison.OrdinalIgnoreCase))
        //                throw new Exception($"ExpiredDate tại dòng {row} trống hoặc là 'Chưa có lô hàng'.");

        //            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double serial))
        //                return DateTime.FromOADate(serial);

        //            var formats = new[]
        //            {
        //        "dd/MM/yyyy","d/M/yyyy","MM/yyyy","MM-yyyy",
        //        "MMM-yyyy","MMMM-yyyy","MMM-yy","MMMM-yy",
        //        "dd-MMM","dd-MMM-yyyy","MMM dd","MMM dd yyyy",
        //        "dd/MM/yy","M/d/yyyy","yyyy-MM-dd"
        //    };

        //            text = text.Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase).Trim();

        //            if (DateTime.TryParseExact(text, formats, new CultureInfo("en-US"), DateTimeStyles.None, out var parsedExact))
        //                return parsedExact;

        //            if (DateTime.TryParse(text, new CultureInfo("en-US"), DateTimeStyles.None, out var parsedLoose))
        //                return parsedLoose;

        //            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedInvariant))
        //                return parsedInvariant;

        //            throw new Exception($"Không parse được ExpiredDate '{text}' tại dòng {row}");
        //        }

        //        // --- Đọc ngày từ Excel ---
        //        var qEDate = ReadDateFromCell(worksheet.Cells[7, 4], "Không thể đọc ngày hết hạn từ Excel");
        //        var qSDate = ReadDateFromCell(worksheet.Cells[7, 2], "Đọc ngày gửi thất bại.");

        //        // --- Kiểm tra hoặc tạo Quotation ---
        //        var existingQuotation = await _unitOfWork.Quotation.Query()
        //            .FirstOrDefaultAsync(q => q.QID == qId && q.SupplierID == supplier.Id);

        //        Quotation quotationToUse;
        //        bool isNewQuotation = false;

        //        if (existingQuotation == null)
        //        {
        //            if (DateTime.Now > qEDate)
        //                return new ServiceResult<int>
        //                {
        //                    StatusCode = 200,
        //                    Message = "Quotation đã quá hạn. Vui lòng yêu cầu nhà cung cấp cập nhật báo giá mới."
        //                };

        //            quotationToUse = new Quotation
        //            {
        //                QID = qId,
        //                SupplierID = supplier.Id,
        //                SendDate = qSDate,
        //                Status = SupplierQuotationStatus.InDate,
        //                QuotationExpiredDate = qEDate,
        //                PRFQID = YC
        //            };

        //            await _unitOfWork.Quotation.AddAsync(quotationToUse);
        //            await _unitOfWork.CommitAsync();
        //            isNewQuotation = true;
        //        }
        //        else
        //        {
        //            if (DateTime.Now > existingQuotation.QuotationExpiredDate)
        //                return new ServiceResult<int>
        //                {
        //                    StatusCode = 200,
        //                    Message = $"Báo giá QID = {qId} của NCC {supplier.Name} đã hết hạn. Vui lòng yêu cầu cập nhật báo giá mới."
        //                };

        //            quotationToUse = existingQuotation;
        //        }

        //        // --- Tạo PO ---
        //        var po = new PurchasingOrder
        //        {
        //            OrderDate = DateTime.Now,
        //            QID = quotationToUse.QID,
        //            UserId = userId,
        //            Total = 0,
        //            Status = PurchasingOrderStatus.sent,
        //        };

        //        await _unitOfWork.PurchasingOrder.AddAsync(po);
        //        await _unitOfWork.CommitAsync();

        //        var products = await _unitOfWork.Product.Query()
        //            .ToDictionaryAsync(p => p.ProductID, p => p.ProductName);

        //        var selectedProductQuantities = input.Details
        //            .Where(d => d.Quantity > 0)
        //            .ToDictionary(d => d.ProductID, d => d.Quantity);

        //        var details = new List<PurchasingOrderDetail>();
        //        var quotationDetails = new List<QuotationDetail>();
        //        int row = 11;

        //        while (!string.IsNullOrEmpty(worksheet.Cells[row, 1].Text))
        //        {
        //            var productIdText = worksheet.Cells[row, 1].Text?.Trim();
        //            if (!int.TryParse(productIdText, out int productId))
        //            {
        //                row++;
        //                continue;
        //            }

        //            var description = worksheet.Cells[row, 3].Text?.Trim();
        //            var dvt = worksheet.Cells[row, 4].Text?.Trim();
        //            var unitPriceText = worksheet.Cells[row, 5].Text?.Trim();
        //            var expiredDateText = worksheet.Cells[row, 6].Text?.Trim();

        //            decimal.TryParse(unitPriceText, out decimal unitPrice);
        //            DateTime expectedExpiredDate = DateTime.MinValue;
        //            try { expectedExpiredDate = ParseDateFromString(expiredDateText, row); } catch { }

        //            var productName = products.ContainsKey(productId) ? products[productId] : "Unknown";

        //            // Nếu người dùng chọn → tạo detail cho PO
        //            if (selectedProductQuantities.TryGetValue(productId, out int quantity))
        //            {
        //                var total = quantity * unitPrice * 1.1m;
        //                details.Add(new PurchasingOrderDetail
        //                {
        //                    POID = po.POID,
        //                    ProductName = productName,
        //                    Description = description,
        //                    DVT = dvt,
        //                    Quantity = quantity,
        //                    UnitPrice = unitPrice,
        //                    UnitPriceTotal = total,
        //                    ExpiredDate = expectedExpiredDate,
        //                    ProductID = productId,
        //                });
        //                po.Total += total;
        //            }

        //            // Nếu là quotation mới → lưu tất cả sản phẩm vào QuotationDetail
        //            if (isNewQuotation)
        //            {
        //                quotationDetails.Add(new QuotationDetail
        //                {
        //                    QID = quotationToUse.QID,
        //                    ProductID = productId,
        //                    ProductName = productName,
        //                    ProductDescription = description ?? string.Empty,
        //                    ProductUnit = dvt ?? string.Empty,
        //                    UnitPrice = unitPrice,
        //                    ProductDate = expectedExpiredDate
        //                });
        //            }

        //            row++;
        //        }

        //        if (details.Count == 0)
        //            return new ServiceResult<int> { StatusCode = 200, Message = "Không có sản phẩm nào được chọn để đặt hàng." };

        //        _unitOfWork.PurchasingOrderDetail.AddRange(details);
        //        if (isNewQuotation && quotationDetails.Count > 0)
        //            _unitOfWork.QuotationDetail.AddRange(quotationDetails);

        //        await _unitOfWork.CommitAsync();

        //        // --- Gửi mail ---
        //        var supplierEmail = worksheet.Cells[5, 6].Text?.Trim();
        //        if (string.IsNullOrWhiteSpace(supplierEmail))
        //            return new ServiceResult<int> { StatusCode = 200, Message = "Kiểm tra lại email nhà cung cấp" };

        //        var poExcelBytes = await GeneratePOExcelAsync(userId, po);
        //        if (purchasingOrderStatus == PurchasingOrderStatus.sent)
        //        {
        //            await _emailService.SendEmailWithAttachmentAsync(
        //                supplierEmail,
        //                "Đơn hàng",
        //                GeneratePOEmailBody(po),
        //                poExcelBytes,
        //                $"PO_{po.POID}.xlsx"
        //            );
        //        }

        //        await _notificationService.SendNotificationToRolesAsync(
        //            userId,
        //            ["ACCOUNTANT"],
        //            "Yêu cầu nhập hàng",
        //            $"Nhân viên {senderUser.UserName} đã gửi mail đặt hàng đến NCC: {supplier.Name}",
        //            Core.Domain.Enums.NotificationType.Reminder
        //        );

        //        await _unitOfWork.CommitTransactionAsync();
        //        try { File.Delete(excelPath); } catch { }

        //        return new ServiceResult<int> { StatusCode = 200, Message = "Thành công." };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "ConvertExcelToPurchaseOrderAsync failed");
        //        await _unitOfWork.RollbackTransactionAsync();
        //        return new ServiceResult<int> { StatusCode = 400, Message = "Thất bại." };
        //    }
        //}        
    }
}
