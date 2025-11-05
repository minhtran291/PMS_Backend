using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.Warehouse
{
    public class WarehouseService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WarehouseService> logger) : Service(unitOfWork, mapper), IWarehouseService
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

        public async Task<ServiceResult<List<LotProductDTO>>> UpdatePhysicalInventoryAsync(string userId, int whlcid, List<PhysicalInventoryUpdateDTO> updates)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var lots = await _unitOfWork.LotProduct.Query()
                    .Where(lp => lp.WarehouselocationID == whlcid)
                    .ToListAsync();

                if (!lots.Any())
                {
                    return new ServiceResult<List<LotProductDTO>>
                    {
                        Data = null,
                        Message = $"Không tìm thấy lô hàng nào trong vị trí kho {whlcid}",
                        StatusCode = 404,
                        Success = false
                    };
                }

                var updatedLots = new List<LotProductDTO>();


                foreach (var update in updates)
                {
                    var lot = lots.FirstOrDefault(l => l.LotID == update.LotID);
                    if (lot != null)
                    {
                        var diff = lot.LotQuantity - update.RealQuantity;
                        var user = await _unitOfWork.Users.UserManager.FindByIdAsync(userId);
                        lot.LotQuantity = update.RealQuantity;
                        lot.lastedUpdate = DateTime.Now;
                        lot.inventoryBy = user?.FullName ?? "Không xác định";
                        lot.Diff = diff;
                        lot.note = update.note;
                        _unitOfWork.LotProduct.Update(lot);


                        var product = await _unitOfWork.Product.Query()
                            .FirstOrDefaultAsync(p => p.ProductID == lot.ProductID);
                        var supplier = await _unitOfWork.Supplier.Query()
                            .FirstOrDefaultAsync(s => s.Id == lot.SupplierID);

                        updatedLots.Add(new LotProductDTO
                        {
                            LotID = lot.LotID,
                            InputDate = lot.InputDate,
                            ExpiredDate = lot.ExpiredDate,
                            InputPrice = lot.InputPrice,
                            SalePrice = lot.SalePrice,
                            LotQuantity = lot.LotQuantity,
                            ProductName = product?.ProductName ?? "Không xác định",
                            SupplierName = supplier?.Name ?? "Không xác định",
                            DiffQuantity = diff,
                            InventoryBy = user?.FullName ?? "Không xác định",
                            LastedUpdate = lot.lastedUpdate,
                            note = lot.note
                        });
                    }
                }

                await _unitOfWork.CommitAsync();


                var affectedProductIds = lots
                    .Select(l => l.ProductID)
                    .Distinct()
                    .ToList();

                foreach (var pid in affectedProductIds)
                {

                    var totalQuantity = await _unitOfWork.LotProduct.Query()
                        .Where(lp => lp.ProductID == pid)
                        .SumAsync(lp => lp.LotQuantity);

                    var product = await _unitOfWork.Product.Query()
                        .FirstOrDefaultAsync(p => p.ProductID == pid);

                    if (product != null)
                    {
                        product.TotalCurrentQuantity = totalQuantity;
                        _unitOfWork.Product.Update(product);
                    }
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<List<LotProductDTO>>
                {
                    Data = updatedLots,
                    Message = $"Đã cập nhật kiểm kê vật lý và đồng bộ tồn kho sản phẩm cho vị trí kho {whlcid}",
                    StatusCode = 200,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật kiểm kê vật lý cho kho {whlcid}", whlcid);

                return new ServiceResult<List<LotProductDTO>>
                {
                    Data = null,
                    Message = "Đã xảy ra lỗi khi kiểm kê vật lý",
                    StatusCode = 400,
                    Success = false
                };
            }
        }
    }
}
