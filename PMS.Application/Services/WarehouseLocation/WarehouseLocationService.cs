using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Services.Base;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Data.UnitOfWork;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace PMS.Application.Services.WarehouseLocation
{
    public class WarehouseLocationService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WarehouseLocationService> logger) : Service(unitOfWork, mapper), IWarehouseLocationService
    {
        private readonly ILogger<WarehouseLocationService> _logger = logger;

        public async Task<ServiceResult<object>> CreateWarehouseLocation(CreateWarehouseLocationDTO dto)
        {
            try
            {
                var warehouse = await _unitOfWork.Warehouse.Query()
                    .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);

                var warehouseValidation = ValidateWarehouse(warehouse); // check kho ton tai
                if (warehouseValidation != null)
                    return warehouseValidation;

                if (warehouse.Status == false)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Nhà kho đang không hoạt động"
                    };

                var locationName = await _unitOfWork.WarehouseLocation.Query()
                    .AnyAsync(wl => wl.LocationName == dto.LocationName.Trim());

                if (locationName)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Tên vị trí đã tồn tại"
                    };

                var newWL = new Core.Domain.Entities.WarehouseLocation
                {
                    WarehouseId = dto.WarehouseId,
                    LocationName = dto.LocationName.Trim(),
                    Status = false
                };

                await _unitOfWork.WarehouseLocation.AddAsync(newWL);
                await _unitOfWork.CommitAsync();

                return new ServiceResult<object>
                {
                    StatusCode = 200,
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

        public async Task<ServiceResult<List<WarehouseLocationDTO>>> GetListWarehouseLocation()
        {
            try
            {
                var list = await _unitOfWork.WarehouseLocation.Query()
                    .AsNoTracking()
                    .ToListAsync();

                var result = _mapper.Map<List<WarehouseLocationDTO>>(list);

                return new ServiceResult<List<WarehouseLocationDTO>>
                {
                    StatusCode = 200,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi");
                return new ServiceResult<List<WarehouseLocationDTO>>
                {
                    StatusCode = 500,
                    Message = "Lỗi"
                };
            }
        }

        public async Task<ServiceResult<object>> UpdateWarehouseLocation(UpdateWarehouseLocationDTO dto)
        {
            try
            {
                var isExisted = await _unitOfWork.WarehouseLocation.Query()
                    .FirstOrDefaultAsync(wl => wl.Id == dto.Id);

                var warehouseLocationValidation = ValidateWarehouseLocation(isExisted); // check location ton tai
                if (warehouseLocationValidation != null)
                    return warehouseLocationValidation;

                var locationName = await _unitOfWork.WarehouseLocation.Query()
                    .AnyAsync(wl => wl.LocationName == dto.LocationName.Trim() && wl.Id != dto.Id);

                // check trung name nhung khac chinh ban than
                if (locationName)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Tên vị trí đã tồn tại"
                    };

                isExisted.LocationName = dto.LocationName.Trim();
                isExisted.Status = dto.Status;

                _unitOfWork.WarehouseLocation.Update(isExisted);
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

        //public async Task<ServiceResult<bool>> StoringLotInWarehouseLocation(StoringLot dto)
        //{
        //    var isExisted = await _unitOfWork.WarehouseLocation.Query()
        //         .FirstOrDefaultAsync(wl => wl.WarehouseId == dto.WarehouseId
        //             && wl.LocationName == dto.LocationName);

        //    if (isExisted == null)
        //    {
        //        _logger.LogError("Loi warehouse location id khong ton tai ham UpdateWarehouseLocation");
        //        return new ServiceResult<bool>
        //        {
        //            Data = false,
        //            Message = $"không tồn tại vị trí kho với ID:{dto.WarehouseId}",
        //            StatusCode = 200
        //        };
        //    }
        //    var exLotProduct = await _unitOfWork.LotProduct.Query().FirstOrDefaultAsync(lp => lp.LotID == dto.LotID);
        //    if (exLotProduct == null)
        //    {
        //        _logger.LogError("loi khi tim kiem lotid");
        //        return new ServiceResult<bool>
        //        {
        //            Data = false,
        //            Message = $"không tồn tại lô sản phẩm với LotID:{dto.LotID}",
        //            StatusCode = 200
        //        };
        //    }
        //    isExisted.LotID = dto.LotID;
        //    _unitOfWork.WarehouseLocation.Update(isExisted);
        //    await _unitOfWork.CommitAsync();
        //    exLotProduct.WarehouselocationID = isExisted.Id;
        //    _unitOfWork.LotProduct.Update(exLotProduct);
        //    await _unitOfWork.CommitAsync();
        //    return new ServiceResult<bool>
        //    {
        //        Data = true,
        //        Message = "Update Thành công",
        //        StatusCode = 200
        //    };
        //}



        public async Task<ServiceResult<object>> ViewWarehouseLocationDetails(int warehouseLocationId)
        {
            try
            {
                var warehouseLocation = await _unitOfWork.WarehouseLocation.Query()
                    .AsNoTracking()
                    .Include(wl => wl.LotProducts)
                        .ThenInclude(lp => lp.Product)
                    .Include(wl => wl.LotProducts)
                        .ThenInclude(lp => lp.Supplier)
                    .FirstOrDefaultAsync(wl => wl.Id == warehouseLocationId);

                var warehouseLocationValidation = ValidateWarehouseLocation(warehouseLocation); // check location ton tai
                if (warehouseLocationValidation != null)
                    return warehouseLocationValidation;

                var result = _mapper.Map<WarehouseLocationDetailsDTO>(warehouseLocation);

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result
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

        public async Task<ServiceResult<object>> GetListByWarehouseId(int warehouseId)
        {
            try
            {
                var warehouse = await _unitOfWork.Warehouse.Query()
                    .AsNoTracking()
                    .Include(w => w.WarehouseLocations)
                    .FirstOrDefaultAsync(w => w.Id == warehouseId);

                if (warehouse == null)
                    return new ServiceResult<object>
                    {
                        StatusCode = 404,
                        Message = "Nhà kho không tồn tại"
                    };

                var result = _mapper.Map<List<WarehouseLocationDTO>>(warehouse.WarehouseLocations.ToList());

                return new ServiceResult<object>
                {
                    StatusCode = 200,
                    Data = result
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

        private static ServiceResult<object>? ValidateWarehouse(Core.Domain.Entities.Warehouse? warehouse)
        {
            if (warehouse == null)
                return new ServiceResult<object>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy nhà kho"
                };

            if (warehouse.Status == false)
                return new ServiceResult<object>
                {
                    StatusCode = 400,
                    Message = "Nhà kho đã dừng hoạt động"
                };

            return null;
        }

        private static ServiceResult<object>? ValidateWarehouseLocation(Core.Domain.Entities.WarehouseLocation? warehouseLocation)
        {
            if (warehouseLocation == null)
                return new ServiceResult<object>
                {
                    StatusCode = 404,
                    Message = "Vị trí trong kho không tồn tại"
                };

            return null;
        }

        public async Task<ServiceResult<object>> DeleteWarehouseLocation(int warehouseLocationId)
        {
            try
            {
                var warehouseLocation = await _unitOfWork.WarehouseLocation.Query()
                    .Include(wl => wl.LotProducts)
                    .FirstOrDefaultAsync(wl => wl.Id == warehouseLocationId);

                var warehouseLocationValidation = ValidateWarehouseLocation(warehouseLocation);
                if (warehouseLocationValidation != null)
                    return warehouseLocationValidation;

                if (warehouseLocation.Status == true)
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Khu thuốc đang hoạt động không thể xóa."
                    };

                if (warehouseLocation.LotProducts.Any())
                    return new ServiceResult<object>
                    {
                        StatusCode = 400,
                        Message = "Khu thuốc đang có các lô hàng không thể xóa."
                    };

                _unitOfWork.WarehouseLocation.Remove(warehouseLocation);
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
    }
}
