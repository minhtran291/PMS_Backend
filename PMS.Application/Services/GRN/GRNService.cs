using AutoMapper;
using DinkToPdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs.GRN;
using PMS.Application.Services.Base;
using PMS.Application.Services.ExternalService;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;
using PMS.Data.Migrations;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.GRNService
{
    public class GRNService(IUnitOfWork unitOfWork, IMapper mapper, IWebHostEnvironment webHostEnvironment, IPdfService pdfService)
        : Service(unitOfWork, mapper), IGRNService
    {
        public async Task<ServiceResult<int>> CreateGoodReceiptNoteFromPOAsync(string userId, int poId, int WarehouseLocationID)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var po = await _unitOfWork.PurchasingOrder.Query()
                    .Include(p => p.PurchasingOrderDetails)
                    .Include(p => p.Quotations)
                    .FirstOrDefaultAsync(p => p.POID == poId);

                if (po == null)
                    return ServiceResult<int>.Fail("Đơn hàng không tồn tại.", 404);

                var quotation = po.Quotations;
                if (quotation == null)
                    return ServiceResult<int>.Fail("Báo giá không tồn tại.", 404);

                var supplier = await _unitOfWork.Supplier.Query()
                    .FirstOrDefaultAsync(sp => sp.Id == quotation.SupplierID);

                if (supplier == null)
                    return ServiceResult<int>.Fail("Không tồn tại nhà cung cấp.", 404);
                var grn = new GoodReceiptNote
                {
                    Source = supplier.Name,
                    CreateBy = userId,
                    CreateDate = DateTime.Now,
                    Total = po.Total,
                    Description = $"Phiếu nhập kho từ đơn hàng PO_{po.POID}",
                    POID = poId,
                    warehouseID = WarehouseLocationID
                };

                await _unitOfWork.GoodReceiptNote.AddAsync(grn);
                await _unitOfWork.CommitAsync();
                var products = await _unitOfWork.Product.Query()
                    .ToDictionaryAsync(p => p.ProductName, p => p, StringComparer.OrdinalIgnoreCase);

                var missingProductNames = po.PurchasingOrderDetails
                    .Select(d => d.ProductName)
                    .Where(name => !products.ContainsKey(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (missingProductNames.Any())
                {
                    return ServiceResult<int>.Fail($"Một số sản phẩm trong PO không tồn tại trong hệ thống: {string.Join(", ", missingProductNames)}", 400);
                }

                var grnDetailEntities = new List<GoodReceiptNoteDetail>();
                var grnDetailsForLot = new List<IGRNDetail>();

                foreach (var detail in po.PurchasingOrderDetails)
                {
                    var product = products[detail.ProductName];

                    var grnDetail = new GoodReceiptNoteDetail
                    {
                        GRNID = grn.GRNID,
                        ProductID = product.ProductID,
                        UnitPrice = detail.UnitPrice,
                        Quantity = detail.Quantity
                    };

                    grnDetailEntities.Add(grnDetail);
                    grnDetailsForLot.Add(new GRNDManuallyDTO
                    {
                        ProductID = product.ProductID,
                        UnitPrice = detail.UnitPrice,
                        Quantity = detail.Quantity,
                        ExpiredDate = detail.ExpiredDate
                    });
                }


                _unitOfWork.GoodReceiptNoteDetail.AddRange(grnDetailEntities);
                await HandleLotProductsAsync(grnDetailsForLot, quotation.SupplierID, grn.CreateDate, WarehouseLocationID);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<int>.SuccessResult(grn.GRNID, "Thành công", 200);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<int>.Fail($"Lỗi khi tạo phiếu nhập kho: {ex.Message}", 500);
            }
        }
       
        private async Task HandleLotProductsAsync(IEnumerable<IGRNDetail> grnDetails, int supplierId, DateTime inputDate, int warehouseLocationID)
        {
            try
            {
                var productIds = grnDetails.Select(d => d.ProductID).Distinct().ToList();

                var existingLots = await _unitOfWork.LotProduct.Query()
                    .Where(lp => productIds.Contains(lp.ProductID) && lp.SupplierID == supplierId)
                    .ToListAsync();

                var newLots = new List<LotProduct>();
                var updatedLots = new List<LotProduct>();

                foreach (var detail in grnDetails)
                {
                    var existingLot = existingLots.FirstOrDefault(lp =>
                        lp.ProductID == detail.ProductID &&
                        lp.SupplierID == supplierId &&
                        lp.ExpiredDate.Date == detail.ExpiredDate.Date &&
                        lp.WarehouselocationID == warehouseLocationID&& 
                        lp.InputPrice==detail.UnitPrice);

                    if (existingLot != null)
                    {
                        existingLot.LotQuantity += detail.Quantity;                       
                        existingLot.InputDate = inputDate;
                        updatedLots.Add(existingLot);
                    }
                    else
                    {
                        newLots.Add(new LotProduct
                        {
                            InputDate = inputDate,
                            ExpiredDate = detail.ExpiredDate,
                            LotQuantity = detail.Quantity,
                            ProductID = detail.ProductID,
                            SupplierID = supplierId,
                            InputPrice = detail.UnitPrice,
                            WarehouselocationID = warehouseLocationID
                        });
                    }
                }

                if (newLots.Any()) _unitOfWork.LotProduct.AddRange(newLots);
                if (updatedLots.Any()) _unitOfWork.LotProduct.UpdateRange(updatedLots);
                await _unitOfWork.CommitAsync();

                var allLots = await _unitOfWork.LotProduct.Query()
                    .Where(lp => productIds.Contains(lp.ProductID))
                    .GroupBy(lp => lp.ProductID)
                    .Select(g => new
                    {
                        ProductID = g.Key,
                        TotalQuantity = g.Sum(lp => lp.LotQuantity)
                    })
                    .ToListAsync();

                foreach (var lotInfo in allLots)
                {
                    var product = await _unitOfWork.Product.Query()
                        .FirstOrDefaultAsync(p => p.ProductID == lotInfo.ProductID);

                    if (product != null)
                    {
                        product.TotalCurrentQuantity = lotInfo.TotalQuantity;
                        _unitOfWork.Product.Update(product);
                    }
                }

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleLotProductsAsync] Lỗi: {ex.Message}");
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Đã xảy ra lỗi khi xử lý lô hàng: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<List<GRNViewDTO>>> GetAllGRN()
        {
            var listGRN = await _unitOfWork.GoodReceiptNote.Query()
                .ToListAsync();

            if (listGRN == null || !listGRN.Any())
            {
                return new ServiceResult<List<GRNViewDTO>>
                {
                    Data = null,
                    Message = "Hiện tại không có bất kỳ bản ghi nhập kho nào",
                    StatusCode = 200,
                    Success = false,
                };
            }


            var warehouseIds = listGRN
                .Select(x => x.warehouseID)
                .Distinct()
                .ToList();

            var warehouseDict = await _unitOfWork.WarehouseLocation.Query()
            .Include(w => w.Warehouse) 
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(
                w => w.Id,
                w => new { LocationName = w.LocationName, WarehouseName = w.Warehouse.Name }
                );

            var userIds = listGRN
                .Select(x => x.CreateBy)
                .Distinct()
                .ToList();

            var userDict = await _unitOfWork.Users.Query()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);


            var result = listGRN.Select(item => new GRNViewDTO
            {
                GRNID = item.GRNID,
                Source = item.Source ?? "Không xác định",
                CreateDate = item.CreateDate,
                Total = item.Total,
                Description = item.Description,
                POID = item.POID,

                CreateBy = userDict.ContainsKey(item.CreateBy)
                    ? userDict[item.CreateBy]
                    : "Không xác định",

                WarehouseName = warehouseDict.ContainsKey(item.warehouseID)
                ? warehouseDict[item.warehouseID].LocationName
                : "Không xác định",

                            warehouse = warehouseDict.ContainsKey(item.warehouseID)
                ? warehouseDict[item.warehouseID].WarehouseName
                : "Không xác định",
            })
            .ToList();

            return new ServiceResult<List<GRNViewDTO>>
            {
                Data = result,
                StatusCode = 200,
                Success = true,
            };
        }

        public async Task<ServiceResult<GRNViewDTO>> GetGRNDetailAsync(int grnId)
        {
            var grn = await _unitOfWork.GoodReceiptNote.Query()
                .Include(g => g.GoodReceiptNoteDetails)
                .FirstOrDefaultAsync(g => g.GRNID == grnId);

            if (grn == null)
            {
                return new ServiceResult<GRNViewDTO>
                {
                    Data = null,
                    Message = $"Không tìm thấy phiếu nhập kho có ID = {grnId}",
                    StatusCode = 404,
                    Success = false
                };
            }

            var productIds = grn.GoodReceiptNoteDetails
                .Select(d => d.ProductID)
                .Distinct()
                .ToList();


            var products = await _unitOfWork.Product.Query()
                .Where(p => productIds.Contains(p.ProductID))
                .ToDictionaryAsync(p => p.ProductID, p => p.ProductName);


            var user = await _unitOfWork.Users.Query()
                .FirstOrDefaultAsync(u => u.Id == grn.CreateBy);


            var warehouse = await _unitOfWork.WarehouseLocation.Query().Include(w=>w.Warehouse)
                .FirstOrDefaultAsync(w => w.Id == grn.warehouseID);


            var detailDtos = grn.GoodReceiptNoteDetails
                .Select(d => new GRNDetailViewDTO
                {
                    GRNDID = d.GRNDID,
                    ProductID = d.ProductID,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    ProductName = products.ContainsKey(d.ProductID)
                        ? products[d.ProductID]
                        : "Không xác định"
                })
                .ToList();


            var grnDto = new GRNViewDTO
            {
                GRNID = grn.GRNID,
                Source = grn.Source ?? "Không xác định",
                CreateDate = grn.CreateDate,
                Total = grn.Total,
                CreateBy = user?.FullName ?? "Không xác định",
                WarehouseName = warehouse?.LocationName ?? "Không xác định",
                warehouse= warehouse?.Warehouse.Name ?? "Không xác định",
                Description = grn.Description,
                POID = grn.POID,
                GRNDetailViewDTO = detailDtos
            };

            return new ServiceResult<GRNViewDTO>
            {
                Data = grnDto,
                StatusCode = 200,
                Success = true,
                Message = "Lấy chi tiết phiếu nhập kho thành công"
            };
        }



        public async Task<byte[]> GeneratePDFGRNAsync(int grnId)
        {
            var grn = await _unitOfWork.GoodReceiptNote.Query()
                .Include(g => g.PurchasingOrder)
                .Include(g => g.PurchasingOrder.Quotations)
                .FirstOrDefaultAsync(g => g.GRNID == grnId);

            if (grn == null)
                throw new Exception($"Không tìm thấy phiếu nhập kho với GRNID = {grnId}");

            var supplier = await _unitOfWork.Supplier.Query()
                .FirstOrDefaultAsync(s => s.Id == grn.PurchasingOrder.Quotations.SupplierID);

            var user = await _unitOfWork.Users.Query()
                .FirstOrDefaultAsync(u => u.Id == grn.CreateBy);

            var warehouselocation = await _unitOfWork.WarehouseLocation.Query()
                .FirstOrDefaultAsync(wl => wl.Id == grn.warehouseID);

            if (warehouselocation == null)
                throw new Exception("Lỗi hệ thống khi tìm kiếm kho chứa");

            var warehouse = await _unitOfWork.Warehouse.Query()
                .Include(w => w.WarehouseLocations)
                .FirstOrDefaultAsync(w => w.Id == warehouselocation.WarehouseId);

            var po = await _unitOfWork.PurchasingOrder.Query()
                .FirstOrDefaultAsync(po => po.POID == grn.POID);

            if (po == null)
                throw new Exception("Lỗi hệ thống khi tìm kiếm đơn hàng nhập");


            string logoPath = Path.Combine(webHostEnvironment.WebRootPath, "assets", "CTTNHHBBPHARMACY.png");
            string backgroundPath = Path.Combine(webHostEnvironment.WebRootPath, "assets", "background.png");

            string logoBase64 = File.Exists(logoPath)
                ? $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(logoPath))}"
                : "";


            string html = $@"
    <html>
    <head>
        <meta charset='UTF-8'>
        <style>
            @page {{
                margin: 0;
            }}
            html, body {{
                width: 100%;
                height: 100%;
                margin: 0;
                padding: 0;
                font-family: Arial, sans-serif;
                font-size: 12pt;
                color: #333;
                position: relative;
                background: url('file:///{backgroundPath.Replace("\\", "/")}') no-repeat center center;
                background-size: cover;
            }}
            body::before {{
                content: """";
                position: absolute;
                top: 0; left: 0; right: 0; bottom: 0;
                background-color: rgba(255, 255, 255, 0.88);
                z-index: 0;
            }}
            .content {{
                position: relative;
                z-index: 1;
                padding: 40px 50px;
            }}
            h1 {{
                background-color: #0066CC;
                color: white;
                text-align: center;
                padding: 10px;
                font-size: 16pt;
                border-radius: 6px;
            }}
            table {{
                width: 100%;
                border-collapse: collapse;
                margin-top: 15px;
            }}
            td, th {{
                border: 1px solid #999;
                padding: 6px 8px;
                vertical-align: middle;
            }}
            th {{
                background-color: #009900;
                color: white;
                text-align: center;
            }}
            .section-title {{
                background-color: #d9d9d9;
                text-align: center;
                font-weight: bold;
                padding: 6px;
                font-size: 13pt;
                margin-top: 15px;
            }}
            .note {{
                font-style: italic;
                text-align: justify;
                margin: 10px 0;
            }}
            .small-note {{
                font-size: 9pt;
                text-align: center;
                color: #666;
                margin-top: 20px;
            }}
            .logo {{
                text-align: left;
                margin-bottom: 10px;
            }}
            .signature-table td {{
                border: none;
                text-align: center;
                vertical-align: bottom;
                height: 80px;
                font-style: italic;
                padding-top: 30px;
            }}
        </style>
    </head>
    <body>
    <div class='content'>
        <div class='logo'>
            {(string.IsNullOrEmpty(logoBase64) ? "" : $"<img src='{logoBase64}' style='height:60px;' />")}
        </div>
        <h1>PHIẾU NHẬP KHO (GOODS RECEIPT NOTE)</h1>
        <h3 style='text-align:center;'>Công ty TNHH Dược phẩm BBPharmacy</h3>
        <table>
            <tr>
                <td><b>Mã GRN:</b></td><td>{grn.GRNID}</td>
                <td><b>Ngày tạo:</b></td><td>{grn.CreateDate:dd/MM/yyyy HH:mm}</td>
            </tr>
            <tr>
                <td><b>Người tạo:</b></td><td>{user?.FullName ?? "Không xác định"}</td>
                <td><b>Theo hợp đồng số:</b></td><td>{grn.POID}, ký ngày {po.OrderDate:dd/MM/yyyy}</td>
            </tr>
            <tr>
                <td><b>Tổng giá trị:</b></td><td>{grn.Total:N2} VNĐ</td>
                <td><b>Nhà cung cấp:</b></td><td>{supplier?.Name ?? grn.Source ?? "Không xác định"}</td>
            </tr>
            <tr>
                <td><b>Nhập tại kho số:</b></td><td>{warehouselocation.LocationName}</td>
                <td><b>Địa điểm:</b></td><td>{warehouse.Name}, {warehouse.Address}</td>
            </tr>
        </table>

        <div class='section-title'>CHI TIẾT HÀNG NHẬP</div>
        <table>
            <thead>
                <tr>
                    <th>STT</th>
                    <th>Mặt hàng</th>
                    <th>Số lượng</th>
                    <th>Đơn giá (VNĐ)</th>
                    <th>Thành tiền (VNĐ)</th>
                </tr>
            </thead>
            <tbody>";

            var details = await _unitOfWork.GoodReceiptNoteDetail.Query()
                .Include(d => d.Product)
                .Where(d => d.GRNID == grn.GRNID)
                .ToListAsync();

            if (details != null && details.Any())
            {
                int index = 1;
                foreach (var d in details)
                {
                    html += $@"
            <tr>
                <td style='text-align:center;'>{index++}</td>
                <td>{d.Product?.ProductName ?? "Không xác định"}</td>
                <td style='text-align:center;'>{d.Quantity}</td>
                <td style='text-align:right;'>{d.UnitPrice:N2}</td>
                <td style='text-align:right;'>{(d.Quantity * d.UnitPrice):N2}</td>
            </tr>";
                }
            }
            else
            {
                html += "<tr><td colspan='5' style='text-align:center;'>Không có dữ liệu chi tiết hàng nhập</td></tr>";
            }


            html += $@"
            </tbody>
        </table>

        <div class='section-title'>GHI CHÚ (NOTES)</div>
        <p class='note'>{grn.Description ?? "Không có ghi chú thêm"}</p>

        <div class='section-title'>CHỮ KÝ XÁC NHẬN</div>
        <table class='signature-table'>
            <tr>
                <td><b>Người lập phiếu</b></td>
                <td><b>Người giao hàng</b></td>
                <td><b>Thủ kho</b></td>
                <td><b>Kế toán trưởng</b></td>
                <td><b>Giám đốc</b></td>
            </tr>
            <tr>
                <td>(Ký, ghi rõ họ tên)</td>
                <td>(Ký, ghi rõ họ tên)</td>
                <td>(Ký, ghi rõ họ tên)</td>
                <td>(Ký, ghi rõ họ tên)</td>
                <td>(Ký, ghi rõ họ tên)</td>
            </tr>
        </table>
        

                   <div class=""small-note"" style=""margin-top: 180px;"">
                (Tệp này được tạo tự động từ hệ thống quản lý kho – vui lòng không chỉnh sửa thủ công)
            </div>
    </div>
    </body>
    </html>";


            var pdfBytes = pdfService.GeneratePdfFromHtml(html);
            return pdfBytes;
        }

        public async Task<ServiceResult<object>> CreateGRNByManually(string userId, int poId, GRNManuallyDTO dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var po = await GetPurchasingOrderAsync(poId);
                if (po == null)
                    return ServiceResult<object>.Fail($"Không tồn tại đơn mua hàng với ID:{poId}", 404);

                var productCheckResult = await ValidateProductsAsync(po, dto);
                if (!productCheckResult.Success)
                    return ServiceResult<object>.Fail(productCheckResult.Message, 400);

                var validationResult = await ValidateQuantityAndPriceAsync(po, dto);
                if (!validationResult.Success)
                    return ServiceResult<object>.Fail(validationResult.Message, validationResult.StatusCode);

                // 4. Tạo GRN
                var newGRN = await CreateGoodReceiptNoteAsync(userId, poId, dto);

                // 5. Tạo chi tiết GRN
                await AddGoodReceiptNoteDetailsAsync(newGRN, dto);

                await HandleLotProductsAsync(
                    dto.GRNDManuallyDTOs.Cast<IGRNDetail>(),
                    po.Quotations?.SupplierID ?? 0,
                    newGRN.CreateDate,
                    dto.WarehouseLocationID
                );

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var updatedRemaining = await CalculateRemainingAfterGRN(po);

                var grnCount = await _unitOfWork.GoodReceiptNote.Query()
                    .CountAsync(s => s.POID == poId);

                return ServiceResult<object>.SuccessResult(
                    new
                    {
                        Message = $"Phiếu nhập kho lần {grnCount} cho PO: {poId} đã tạo thành công",
                        Remaining = updatedRemaining
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception(ex.Message);
            }
        }

        private async Task<PurchasingOrder?> GetPurchasingOrderAsync(int poId)
        {
            return await _unitOfWork.PurchasingOrder.Query()
                .Include(p => p.PurchasingOrderDetails)
                .Include(p => p.Quotations)
                .FirstOrDefaultAsync(p => p.POID == poId);
        }


        private async Task<ServiceResult<bool>> ValidateProductsAsync(PurchasingOrder po, GRNManuallyDTO dto)
        {
            var poProductNames = po.PurchasingOrderDetails
                .Select(x => x.ProductName.Trim().ToLower())
                .ToHashSet();

            var grnProductIds = dto.GRNDManuallyDTOs.Select(x => x.ProductID).ToList();

            var grnProducts = await _unitOfWork.Product.Query()
                .Where(p => grnProductIds.Contains(p.ProductID))
                .Select(p => new { p.ProductID, p.ProductName })
                .ToListAsync();

            var missingProducts = grnProductIds.Except(grnProducts.Select(p => p.ProductID)).ToList();
            if (missingProducts.Any())
                return ServiceResult<bool>.Fail(
                    $"Một số sản phẩm không tồn tại trong hệ thống: {string.Join(", ", missingProducts)}",
                    400
                );

            var invalidProducts = grnProducts
                .Where(p => !poProductNames.Contains(p.ProductName.Trim().ToLower()))
                .Select(p => p.ProductName)
                .ToList();

            if (invalidProducts.Any())
                return ServiceResult<bool>.Fail(
                    $"Một số sản phẩm không thuộc đơn mua hàng này: {string.Join(", ", invalidProducts)}",
                    400
                );

            return ServiceResult<bool>.SuccessResult(true);
        }


        private async Task<ServiceResult<List<RemainingPOItemDTO>>> ValidateQuantityAndPriceAsync(PurchasingOrder po, GRNManuallyDTO dto)
        {
            var remainingList = new List<RemainingPOItemDTO>();

            foreach (var grnItem in dto.GRNDManuallyDTOs)
            {
                var poDetail = po.PurchasingOrderDetails
                    .FirstOrDefault(d => d.ProductID == grnItem.ProductID);

                if (poDetail == null)
                    return ServiceResult<List<RemainingPOItemDTO>>.Fail(
                        $"Sản phẩm ID {grnItem.ProductID} không tồn tại trong đơn mua hàng.",
                        400
                    );

                if (grnItem.UnitPrice != poDetail.UnitPrice)
                    return ServiceResult<List<RemainingPOItemDTO>>.Fail(
                        $"Đơn giá sản phẩm '{poDetail.ProductName}' không khớp với đơn hàng.",
                        400
                    );

                var totalReceivedBefore = await _unitOfWork.GoodReceiptNoteDetail.Query()
                    .Include(d => d.GoodReceiptNote)
                    .Where(d => d.ProductID == grnItem.ProductID && d.GoodReceiptNote.POID == po.POID)
                    .SumAsync(d => (decimal?)d.Quantity) ?? 0;

                var remainingQty = poDetail.Quantity - totalReceivedBefore;

                // THÊM VÀO DANH SÁCH TRẢ VỀ
                remainingList.Add(new RemainingPOItemDTO
                {
                    ProductID = poDetail.ProductID,
                    ProductName = poDetail.ProductName,
                    OrderedQty = poDetail.Quantity,
                    ReceivedQty = totalReceivedBefore,
                    RemainingQty = remainingQty
                });

                if (remainingQty <= 0)
                    return ServiceResult<List<RemainingPOItemDTO>>.Fail(
                        $"Sản phẩm '{poDetail.ProductName}' đã nhập đủ. Không thể nhập thêm.",
                        400
                    );

                if (grnItem.Quantity > remainingQty)
                    return ServiceResult<List<RemainingPOItemDTO>>.Fail(
                        $"Sản phẩm '{poDetail.ProductName}' chỉ còn lại {remainingQty} để nhập.",
                        400
                    );
            }

            return ServiceResult<List<RemainingPOItemDTO>>.SuccessResult(remainingList, "OK");
        }


        private async Task<GoodReceiptNote> CreateGoodReceiptNoteAsync(string userId, int poId, GRNManuallyDTO dto)
        {
            var total = dto.GRNDManuallyDTOs.Sum(x => x.Quantity * x.UnitPrice);

            var grn = new GoodReceiptNote
            {
                Source = dto.Source,
                CreateDate = DateTime.Now,
                Total = total,
                CreateBy = userId,
                Description = dto.Description,
                POID = poId,
                warehouseID= dto.WarehouseLocationID,
            };

            await _unitOfWork.GoodReceiptNote.AddAsync(grn);
            await _unitOfWork.CommitAsync(); // để có GRNID

            return grn;
        }


        private async Task AddGoodReceiptNoteDetailsAsync(GoodReceiptNote grn, GRNManuallyDTO dto)
        {
            var details = dto.GRNDManuallyDTOs.Select(d => new GoodReceiptNoteDetail
            {
                ProductID = d.ProductID,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                GRNID = grn.GRNID
            }).ToList();

            _unitOfWork.GoodReceiptNoteDetail.AddRange(details);
            await _unitOfWork.CommitAsync();
        }


        private async Task<List<RemainingPOItemDTO>> CalculateRemainingAfterGRN(PurchasingOrder po)
        {
            var poProductIds = po.PurchasingOrderDetails.Select(x => x.ProductID).ToList();

            var receivedDict = await _unitOfWork.GoodReceiptNoteDetail.Query()
                .Include(x => x.GoodReceiptNote)
                .Where(x => poProductIds.Contains(x.ProductID) && x.GoodReceiptNote.POID == po.POID)
                .GroupBy(x => x.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    ReceivedQty = g.Sum(x => x.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.ReceivedQty);

            return po.PurchasingOrderDetails.Select(d =>
            {
                receivedDict.TryGetValue(d.ProductID, out int receivedQty);

                return new RemainingPOItemDTO
                {
                    ProductID = d.ProductID,
                    ProductName = d.ProductName,
                    OrderedQty = d.Quantity,
                    ReceivedQty = receivedQty,
                    RemainingQty = d.Quantity - receivedQty
                };
            }).ToList();
        }
    }
}























