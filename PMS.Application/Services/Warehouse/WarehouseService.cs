using System.Drawing;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.Product;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.Warehouse
{
    public class WarehouseService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WarehouseService> logger, IWebHostEnvironment webHostEnvironment, IPdfService pdfService) : Service(unitOfWork, mapper), IWarehouseService
    {
        private readonly ILogger<WarehouseService> _logger = logger;

        public async Task<ServiceResult<object>> CreateWarehouseAsync(CreateWarehouseDTO dto)
        {
            try
            {
                var validateName = await _unitOfWork.Warehouse.Query()
                .AnyAsync(w => w.Name == dto.Name.Trim());

                if (validateName)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Tên kho đã tồn tại"
                    };

                var warehouse = new Core.Domain.Entities.Warehouse
                {
                    Name = dto.Name.Trim(),
                    Address = dto.Address.Trim(),
                    Status = false,
                };

                await _unitOfWork.Warehouse.AddAsync(warehouse);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 201,
                    Message = "Tạo thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<List<WarehouseDTO>>> GetListWarehouseAsync()
        {
            try
            {
                var list = await _unitOfWork.Warehouse.Query()
                    .AsNoTracking()
                    .ToListAsync();

                var result = _mapper.Map<List<WarehouseDTO>>(list);

                return new ServiceResult<List<WarehouseDTO>>
                {
                    StatusCode = 200,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<List<WarehouseDTO>>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> UpdateWarehouseAsync(UpdateWarehouseDTO dto)
        {
            try
            {
                var isExisted = await _unitOfWork.Warehouse.Query()
                .FirstOrDefaultAsync(w => w.Id == dto.Id);

                if (isExisted == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy nhà kho"
                    };

                var validateName = await _unitOfWork.Warehouse.Query()
                    .AnyAsync(w => w.Name == dto.Name.Trim() && w.Id != dto.Id);

                if (validateName)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Tên nhà kho đã tồn tại"
                    };

                isExisted.Name = dto.Name.Trim();
                isExisted.Address = dto.Address.Trim();
                isExisted.Status = dto.Status;

                _unitOfWork.Warehouse.Update(isExisted);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Cập nhật thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<WarehouseDetailsDTO>> ViewWarehouseDetailsAysnc(int warehouseId)
        {
            try
            {
                var isExisted = await _unitOfWork.Warehouse.Query()
                    .AsNoTracking()
                    .Include(w => w.WarehouseLocations)
                    .FirstOrDefaultAsync(w => w.Id == warehouseId);

                if (isExisted == null)
                    return new ServiceResult<WarehouseDetailsDTO>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy nhà kho"
                    };

                var result = _mapper.Map<WarehouseDetailsDTO>(isExisted);

                return new ServiceResult<WarehouseDetailsDTO>
                {
                    StatusCode = 200,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<WarehouseDetailsDTO>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> DeleteWarehouseAsync(int warehouseId)
        {
            try
            {
                var warehouse = await _unitOfWork.Warehouse.Query()
                .Include(w => w.WarehouseLocations)
                .FirstOrDefaultAsync(w => w.Id == warehouseId);

                if (warehouse == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Không tìm thấy nhà kho"
                    };

                if (warehouse.Status == true)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Nhà kho đang hoạt động không thể xóa"
                    };

                if (warehouse.WarehouseLocations.Any())
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Nhà kho đang chứa các khu thuốc không thể xóa"
                    };

                _unitOfWork.Warehouse.Remove(warehouse);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Message = "Xóa thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<object>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<List<LotProductDTO>>> GetAllLotByWHLID(int whlcid)
        {
            try
            {
                var listLotp = await _unitOfWork.LotProduct.Query().Where(lp => lp.WarehouselocationID == whlcid).ToListAsync();
                var result = new List<LotProductDTO>();
                if (!listLotp.Any())
                {
                    return new ServiceResult<List<LotProductDTO>>
                    {
                        Data = null,
                        Message = $"Hiện tại không tìm thấy bất kỳ sản phẩm nào ở vị trí {whlcid}",
                        StatusCode = 200,
                        Success = false
                    };
                }
                foreach (var p in listLotp)
                {
                    var product = await _unitOfWork.Product.Query().FirstOrDefaultAsync(pr => pr.ProductID == p.ProductID);
                    var sup = await _unitOfWork.Supplier.Query().FirstOrDefaultAsync(sp => sp.Id == p.SupplierID);
                    result.Add(new LotProductDTO
                    {
                        LotID = p.LotID,
                        ProductName = product.ProductName,
                        SupplierName = sup.Name,
                        InputDate = p.InputDate,
                        ExpiredDate = p.ExpiredDate,
                        InputPrice = p.InputPrice,
                        SalePrice = p.SalePrice,
                        LotQuantity = p.LotQuantity,
                    });
                }
                return new ServiceResult<List<LotProductDTO>>
                {
                    Data = result,
                    Message = $"thành công lấy dữ liệu tại vị trí {whlcid}",
                    StatusCode = 200,
                    Success = true,

                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý dữ liệu GetAllLotByWHLID");
                return new ServiceResult<List<LotProductDTO>>
                {
                    StatusCode = 400,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<LotProductDTO>> UpdateSalePriceAsync(int whlcid, int lotid, decimal newSalePrice)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                var lotProduct = await _unitOfWork.LotProduct.Query()
                    .FirstOrDefaultAsync(lp => lp.WarehouselocationID == whlcid && lp.LotID == lotid);

                if (lotProduct == null)
                {
                    return new ServiceResult<LotProductDTO>
                    {
                        StatusCode = 404,
                        Success = false,
                        Message = $"Không tìm thấy sản phẩm (ở lot{lotid}) trong vị trí kho (WHLID = {whlcid})."
                    };
                }
                if (newSalePrice >= lotProduct.InputPrice)
                {

                    lotProduct.SalePrice = newSalePrice;
                    _unitOfWork.LotProduct.Update(lotProduct);
                    await _unitOfWork.CommitAsync();


                    var product = await _unitOfWork.Product.Query().FirstOrDefaultAsync(p => p.ProductID == lotProduct.ProductID);
                    var supplier = await _unitOfWork.Supplier.Query().FirstOrDefaultAsync(s => s.Id == lotProduct.SupplierID);

                    var resultDto = new LotProductDTO
                    {
                        LotID = lotProduct.LotID,
                        ProductName = product?.ProductName,
                        SupplierName = supplier?.Name,
                        InputDate = lotProduct.InputDate,
                        ExpiredDate = lotProduct.ExpiredDate,
                        InputPrice = lotProduct.InputPrice,
                        SalePrice = lotProduct.SalePrice,
                        LotQuantity = lotProduct.LotQuantity
                    };
                    await _unitOfWork.CommitTransactionAsync();
                    return new ServiceResult<LotProductDTO>
                    {
                        StatusCode = 200,
                        Success = true,
                        Message = $"Cập nhật giá bán thành công cho sản phẩm ở lot {lotid}.",
                        Data = resultDto
                    };

                }
                else
                {
                    return new ServiceResult<LotProductDTO>
                    {
                        StatusCode = 400,
                        Success = true,
                        Message = "Gía bán yêu cầu lớn hơn giá nhập",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật giá bán sản phẩm ProductID = {productId} trong WHLID = {whlcid}", lotid, whlcid);

                return new ServiceResult<LotProductDTO>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Đã xảy ra lỗi trong quá trình cập nhật giá bán."
                };
            }
        }

        public async Task<ServiceResult<byte[]>> ExportInventorySessionToExcelAsync(string userId, int sessionId)
        {
            var historiesResult = await GetHistoriesBySessionIdAsync(sessionId);

            if (!historiesResult.Success || !historiesResult.Data.Any())
                return ServiceResult<byte[]>.Fail("Không có dữ liệu kiểm kê để xuất file.");
            var ses = await _unitOfWork.InventorySession.Query().FirstOrDefaultAsync(p => p.InventorySessionID == sessionId);
            if (ses == null) { return ServiceResult<byte[]>.Fail("Lỗi khi tìm kiếm theo phiên"); }
            var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("InventoryHistories");


            ws.Cells["A1:H1"].Merge = true;
            ws.Cells["A1"].Value = "CÔNG TY TNHH DƯỢC PHẨM BBPHARMACY";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;


            ws.Cells["A2:H2"].Merge = true;
            ws.Cells["A2"].Value = "BIÊN BẢN KIỂM KÊ SẢN PHẨM, HÀNG HÓA";
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.Font.Size = 13;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;



            int infoStartRow = 5;


            ws.Cells[infoStartRow, 1, infoStartRow, 4].Merge = true;
            ws.Cells[infoStartRow, 1].Value = $"Người kiểm tra: {user?.FullName ?? "Không xác định"}";
            ws.Cells[infoStartRow, 1].Style.Font.Italic = true;
            ws.Cells[infoStartRow, 1].Style.Font.Size = 11;
            ws.Cells[infoStartRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;


            ws.Cells[infoStartRow + 1, 1, infoStartRow + 1, 4].Merge = true;
            ws.Cells[infoStartRow + 1, 1].Value = $"Phiên kiểm kê: {ses.InventorySessionID}";
            ws.Cells[infoStartRow + 1, 1].Style.Font.Italic = true;
            ws.Cells[infoStartRow + 1, 1].Style.Font.Size = 11;
            ws.Cells[infoStartRow + 1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;


            ws.Cells[infoStartRow, 5, infoStartRow, 8].Merge = true;
            ws.Cells[infoStartRow, 5].Value = $"Ngày kiểm tra: {ses.StartDate:dd/MM/yyyy}";
            ws.Cells[infoStartRow, 5].Style.Font.Italic = true;
            ws.Cells[infoStartRow, 5].Style.Font.Size = 11;
            ws.Cells[infoStartRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;


            ws.Cells[infoStartRow + 1, 5, infoStartRow + 1, 8].Merge = true;
            ws.Cells[infoStartRow + 1, 5].Value = $"Ngày kết thúc: {ses.EndDate:dd/MM/yyyy}";
            ws.Cells[infoStartRow + 1, 5].Style.Font.Italic = true;
            ws.Cells[infoStartRow + 1, 5].Style.Font.Size = 11;
            ws.Cells[infoStartRow + 1, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;


            using (var range = ws.Cells[infoStartRow, 1, infoStartRow + 1, 8])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 240));
            }

            ws.Cells["A3:H3"].Style.Font.Italic = true;


            int headerRow = 8;
            ws.Cells[headerRow, 1].Value = "LÔ SP";
            ws.Cells[headerRow, 2].Value = "TÊN SP";
            ws.Cells[headerRow, 3].Value = "SỐ LƯỢNG KÊ BIÊN";
            ws.Cells[headerRow, 4].Value = "SỐ LƯỢNG THỰC TẾ";
            ws.Cells[headerRow, 5].Value = "CHÊNH LỆCH";
            ws.Cells[headerRow, 6].Value = "GHI CHÚ";
            ws.Cells[headerRow, 7].Value = "PHỤ TRÁCH";
            ws.Cells[headerRow, 8].Value = "NGÀY KIỂM KÊ";


            using (var range = ws.Cells[headerRow, 1, headerRow, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }


            int row = headerRow + 1;
            foreach (var h in historiesResult.Data)
            {
                var checker = await _unitOfWork.Users.UserManager.FindByIdAsync(h.InventoryBy);
                ws.Cells[row, 1].Value = h.LotID;
                ws.Cells[row, 2].Value = h.ProductName;
                ws.Cells[row, 3].Value = h.SystemQuantity;
                ws.Cells[row, 4].Value = h.ActualQuantity;
                ws.Cells[row, 5].Value = h.Diff;
                ws.Cells[row, 6].Value = h.Note;
                ws.Cells[row, 7].Value = checker?.FullName ?? "Không xác định";
                ws.Cells[row, 8].Value = h.LastUpdated.ToString("dd/MM/yyyy HH:mm");

                row++;
            }


            ws.Cells[ws.Dimension.Address].AutoFitColumns();


            int footerRow = row + 2;
            ws.Cells[footerRow, 1, footerRow, 8].Merge = true;
            ws.Cells[footerRow, 1].Value = "Ghi chú: Biên bản lập theo Thông tư 133/2016/TT-BTC ngày 26/3/2016 của Bộ Tài chính.";
            ws.Cells[footerRow, 1].Style.Font.Italic = true;
            ws.Cells[footerRow, 1].Style.Font.Size = 10;
            ws.Cells[footerRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            var excelBytes = package.GetAsByteArray();
            return ServiceResult<byte[]>.SuccessResult(excelBytes);
        }

        public async Task<ServiceResult<int>> CreateInventorySessionAsync(string userId, int whlcid)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {

                var lots = await _unitOfWork.LotProduct.Query()
                    .Include(l => l.Product)
                    .Where(l => l.WarehouselocationID == whlcid)
                    .ToListAsync();

                if (!lots.Any())
                    return ServiceResult<int>.Fail("Không có lô sản phẩm nào để kiểm kê.");


                var session = new InventorySession
                {
                    StartDate = DateTime.Now,
                    CreatedBy = userId,
                    Status = InventorySessionStatus.Draft,
                };
                await _unitOfWork.InventorySession.AddAsync(session);
                await _unitOfWork.CommitAsync();


                var histories = lots.Select(l => new InventoryHistory
                {
                    InventorySessionID = session.InventorySessionID,
                    LotID = l.LotID,
                    SystemQuantity = l.LotQuantity,
                    ActualQuantity = 0,
                    LastUpdated = DateTime.Now,
                    InventoryBy = userId,
                    Note = null
                }).ToList();

                await _unitOfWork.InventoryHistory.AddRangeAsync(histories);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();


                return ServiceResult<int>.SuccessResult(session.InventorySessionID, "Tạo phiên kiểm kê thành công!");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<int>.Fail($"Lỗi khi tạo phiên kiểm kê: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UpdateInventoryBatchAsync(string userId, UpdateInventoryBatchDto input)
        {
            if (input.LotCounts == null || !input.LotCounts.Any())
                return ServiceResult<bool>.Fail("Không có dữ liệu lô để cập nhật.");


            var historyIds = input.LotCounts.Select(l => l.HistoryId).ToList();
            var histories = await _unitOfWork.InventoryHistory.Query()
                .Include(h => h.InventorySession)
                .Where(h => historyIds.Contains(h.InventoryHistoryID))
                .ToListAsync();

            if (!histories.Any())
                return ServiceResult<bool>.Fail("Không tìm thấy bất kỳ lịch sử kiểm kê nào.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var lot in input.LotCounts)
                {
                    var history = histories.FirstOrDefault(h => h.InventoryHistoryID == lot.HistoryId);
                    if (history == null) continue;

                    if (history.InventorySession.Status == InventorySessionStatus.Completed)
                        return ServiceResult<bool>.Fail($"Phiên kiểm kê đã hoàn tất, không thể chỉnh sửa HistoryID {lot.HistoryId}.");

                    history.ActualQuantity = lot.ActualQuantity;
                    history.LastUpdated = DateTime.Now;
                    history.InventoryBy = userId;
                    history.Note = lot.Note;
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<bool>.SuccessResult(true, "Cập nhật số lượng thực tế nhiều lô thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<bool>.Fail($"Lỗi khi cập nhật số lượng nhiều lô: {ex.Message}");
            }
        }



        public async Task<ServiceResult<IEnumerable<InventoryCompareDTO>>> GetInventoryComparisonAsync(int sessionId)
        {
            var histories = await _unitOfWork.InventoryHistory.Query()
                .Include(h => h.LotProduct)
                    .ThenInclude(l => l.Product)
                .Where(h => h.InventorySessionID == sessionId)
                .ToListAsync();

            if (!histories.Any())
                return ServiceResult<IEnumerable<InventoryCompareDTO>>.Fail("Không có dữ liệu kiểm kê.");

            var result = histories.Select(h => new InventoryCompareDTO
            {
                LotID = h.LotID,
                ProductName = h.LotProduct.Product.ProductName,
                SystemQuantity = h.SystemQuantity,
                ActualQuantity = h.ActualQuantity,
                Diff = h.ActualQuantity - h.SystemQuantity,
                Note = h.Note
            });

            return ServiceResult<IEnumerable<InventoryCompareDTO>>.SuccessResult(result);
        }


        public async Task<ServiceResult<int>> CompleteInventorySessionAsync(int sessionId, string userId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1️⃣ Lấy phiên kiểm kê
                var session = await _unitOfWork.InventorySession.Query()
                    .Include(s => s.InventoryHistories)
                        .ThenInclude(h => h.LotProduct)
                    .ThenInclude(lp => lp.Product)
                    .FirstOrDefaultAsync(s => s.InventorySessionID == sessionId);

                if (session == null)
                    return ServiceResult<int>.Fail("Không tìm thấy phiên kiểm kê.");

                if (!session.InventoryHistories.Any())
                    return ServiceResult<int>.Fail("Phiên kiểm kê chưa có dữ liệu.");

                // 2️⃣ Cập nhật LotProduct.LotQuantity = ActualQuantity
                foreach (var history in session.InventoryHistories)
                {
                    var lot = history.LotProduct;
                    if (lot == null) continue;

                    lot.LotQuantity = history.ActualQuantity;
                    lot.LastCheckedDate = DateTime.Now;
                }

                // 3️⃣ Cập nhật tổng tồn của từng Product
                var productGroups = session.InventoryHistories
                    .Where(h => h.LotProduct != null)
                    .GroupBy(h => h.LotProduct.Product);

                foreach (var group in productGroups)
                {
                    var product = group.Key;
                    product.TotalCurrentQuantity = group
                        .Sum(h => h.ActualQuantity);
                }

                // 4️⃣ Cập nhật trạng thái phiên kiểm kê
                session.Status = InventorySessionStatus.Completed;
                session.EndDate = DateTime.Now;

                // 5️⃣ Lưu DB
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<int>.SuccessResult(1, "Hoàn tất kiểm kê thành công!");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<int>.Fail($"Lỗi khi hoàn tất kiểm kê: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<InventoryHistoryDTO>>> GetHistoriesBySessionIdAsync(int sessionId)
        {
            var histories = await _unitOfWork.InventoryHistory.Query()
        .Include(h => h.LotProduct)
            .ThenInclude(lp => lp.Product)
        .Where(h => h.InventorySessionID == sessionId)
        .ToListAsync();

            if (!histories.Any())
                return ServiceResult<IEnumerable<InventoryHistoryDTO>>.Fail("Không tìm thấy lịch sử kiểm kê nào cho phiên này.");

            var result = histories.Select(h => new InventoryHistoryDTO
            {
                InventoryHistoryID = h.InventoryHistoryID,
                LotID = h.LotID,
                ProductName = h.LotProduct?.Product?.ProductName ?? "",
                SystemQuantity = h.SystemQuantity,
                ActualQuantity = h.ActualQuantity,
                Note = h.Note,
                InventoryBy = h.InventoryBy,
                LastUpdated = h.LastUpdated
            });

            return ServiceResult<IEnumerable<InventoryHistoryDTO>>.SuccessResult(result);
        }
    }

}

