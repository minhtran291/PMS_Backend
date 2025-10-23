using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.DTO.Content;
using PMS.Data.Migrations;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.GRNService
{
    public class GRNService(IUnitOfWork unitOfWork, IMapper mapper)
        : Service(unitOfWork, mapper), IGRNService
    {
        public async Task<ServiceResult<int>> CreateGoodReceiptNoteFromPOAsync(string userId, int poId)
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
                    POID = poId
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
                await HandleLotProductsAsync(grnDetailsForLot, quotation.SupplierID, grn.CreateDate);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<int>.SuccessResult(grn.GRNID, "Thành công", 201);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<int>.Fail($"Lỗi khi tạo phiếu nhập kho: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResult<bool>> CreateGRNByManually(string userId, int poId, GRNManuallyDTO GRNManuallyDTO)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var po = await _unitOfWork.PurchasingOrder.Query()
                    .Include(p => p.PurchasingOrderDetails)
                    .FirstOrDefaultAsync(p => p.POID == poId);

                if (po == null)
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        Message = $"Không tồn tại đơn mua hàng với ID:{poId}",
                        StatusCode = 404
                    };
                }

                // Danh sách sản phẩm thuộc PO
                var poProductNames = po.PurchasingOrderDetails
                    .Select(x => x.ProductName.Trim().ToLower())
                    .ToHashSet();

                // Danh sách ProductID từ DTO
                var grnProductIds = GRNManuallyDTO.GRNDManuallyDTOs.Select(x => x.ProductID).ToList();

                // Lấy thông tin sản phẩm từ bảng Product
                var grnProducts = await _unitOfWork.Product.Query()
                    .Where(p => grnProductIds.Contains(p.ProductID))
                    .Select(p => new { p.ProductID, p.ProductName })
                    .ToListAsync();

                // Kiểm tra sản phẩm không tồn tại trong hệ thống
                var missingProducts = grnProductIds.Except(grnProducts.Select(p => p.ProductID)).ToList();
                if (missingProducts.Any())
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        Message = $"Một số sản phẩm không tồn tại trong hệ thống: {string.Join(", ", missingProducts)}",
                        StatusCode = 400
                    };
                }

                // Kiểm tra sản phẩm không thuộc PO
                var invalidProducts = grnProducts
                    .Where(p => !poProductNames.Contains(p.ProductName.Trim().ToLower()))
                    .Select(p => p.ProductName)
                    .ToList();

                if (invalidProducts.Any())
                {
                    return new ServiceResult<bool>
                    {
                        Data = false,
                        Message = $"Một số sản phẩm không thuộc đơn mua hàng này: {string.Join(", ", invalidProducts)}",
                        StatusCode = 400
                    };
                }

                // Tạo phiếu nhập kho
                var total = GRNManuallyDTO.GRNDManuallyDTOs.Sum(x => x.Quantity * x.UnitPrice);
                var newGRN = new GoodReceiptNote
                {
                    Source = GRNManuallyDTO.Source,
                    CreateDate = DateTime.Now,
                    Total = total,
                    CreateBy = userId,
                    Description = GRNManuallyDTO.Description,
                    POID = poId,
                };

                await _unitOfWork.GoodReceiptNote.AddAsync(newGRN);

                // Thêm chi tiết phiếu nhập
                var GRND = GRNManuallyDTO.GRNDManuallyDTOs.Select(grnd => new GoodReceiptNoteDetail
                {
                    ProductID = grnd.ProductID,
                    UnitPrice = grnd.UnitPrice,
                    Quantity = grnd.Quantity,
                    GRNID = newGRN.GRNID
                }).ToList();

                _unitOfWork.GoodReceiptNoteDetail.AddRange(GRND);
                await HandleLotProductsAsync(GRNManuallyDTO.GRNDManuallyDTOs.Cast<IGRNDetail>(), po.Quotations.SupplierID, newGRN.CreateDate);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Đếm số phiếu nhập
                var grnCount = await _unitOfWork.GoodReceiptNote.Query()
                    .CountAsync(s => s.POID == poId);

                return new ServiceResult<bool>
                {
                    Data = true,
                    Message = $"Phiếu nhập kho lần {grnCount} cho PO: {poId} đã tạo thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception(ex.Message);
            }
        }

        private async Task HandleLotProductsAsync(IEnumerable<IGRNDetail> grnDetails, int supplierId, DateTime inputDate)
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
                    lp.ExpiredDate.Date == detail.ExpiredDate.Date);

                if (existingLot != null)
                {
                    if (existingLot.LotQuantity == 0)
                    {
                        existingLot.ExpiredDate=detail.ExpiredDate;
                        existingLot.SupplierID = supplierId;
                        existingLot.InputDate = inputDate;
                        existingLot.LotQuantity = detail.Quantity;
                        existingLot.ProductID=detail.ProductID;
                        existingLot.InputPrice=detail.UnitPrice;
                        updatedLots.Add(existingLot);

                    }
                    if (existingLot != null)
                    {
                        existingLot.LotQuantity += detail.Quantity;
                        existingLot.InputPrice = detail.UnitPrice;
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
                            InputPrice = detail.UnitPrice
                        });
                    }
                }
                else
                {
                    throw new Exception("Lỗi khi tìm kiếm lô sản phẩm");
                }


                // Cập nhật tồn kho sản phẩm
                var product = await _unitOfWork.Product.Query()
                    .FirstOrDefaultAsync(p => p.ProductID == detail.ProductID);

                if (product != null)
                {
                    product.TotalCurrentQuantity += detail.Quantity;
                    _unitOfWork.Product.Update(product);
                }
            }

            if (newLots.Any()) _unitOfWork.LotProduct.AddRange(newLots);
            if (updatedLots.Any()) _unitOfWork.LotProduct.UpdateRange(updatedLots);
        }
    }
}























