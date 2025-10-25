using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Services.Base;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Data.UnitOfWork;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.WarehouseLocation
{
    public class WarehouseLocationService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WarehouseLocationService> logger) : Service(unitOfWork, mapper), IWarehouseLocationService
    {
        private readonly ILogger<WarehouseLocationService> _logger = logger;

        public async Task CreateWarehouseLocation(CreateWarehouseLocation dto)
        {
            var checkWarehouse = await _unitOfWork.Warehouse.Query()
                .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);

            if (checkWarehouse == null)
            {
                _logger.LogError("Khong tim thay id cua warehouse ham CreateWarehouseLocation");
                throw new Exception("Có lỗi xảy ra");
            }

            var newWL = new Core.Domain.Entities.WarehouseLocation
            {
                WarehouseId = dto.WarehouseId,
                LocationName = dto.LocationName,
                Status = dto.Status,
            };

            await _unitOfWork.WarehouseLocation.AddAsync(newWL);
            await _unitOfWork.CommitAsync();
        }

        public async Task<List<WarehouseLocationList>> GetListWarehouseLocation()
        {
            var list = await _unitOfWork.WarehouseLocation.Query()
                .ToListAsync();

            return list.Select(w => new WarehouseLocationList
            {
                Id = w.Id,
                LocationName= w.LocationName,
                Status = w.Status
            }).ToList();
        }

        public async Task UpdateWarehouseLocation(UpdateWarehouseLocation dto)
        {
            var isExisted = await _unitOfWork.WarehouseLocation.Query()
                .FirstOrDefaultAsync(wl => wl.Id == dto.WarehouseId);

            if (isExisted == null)
            {
                _logger.LogError("Loi warehouse location id khong ton tai ham UpdateWarehouseLocation");
                throw new Exception("Có lỗi xảy ra");
            }

            isExisted.LocationName = dto.LocationName;
            isExisted.Status = dto.Status;

            _unitOfWork.WarehouseLocation.Update(isExisted);
            await _unitOfWork.CommitAsync();
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



        public async Task<WarehouseLocationList> ViewWarehouseLocationDetails(int warehouseLocationId)
        {
            var isExisted = await _unitOfWork.WarehouseLocation.Query()
                .FirstOrDefaultAsync(wl => wl.Id == warehouseLocationId);

            if (isExisted == null)
            {
                _logger.LogError("Loi warehouse location id khong ton tai ham ViewWarehouseLocationDetails");
                throw new Exception("Có lỗi xảy ra");
            }

            return new WarehouseLocationList
            {
                Id = isExisted.Id,
                LocationName = isExisted.LocationName,
                Status = isExisted.Status,
            };
        }

        public async Task<List<WarehouseLocationList>> GetListByWarehouseId(int warehouseId)
        {
            var isExisted = await _unitOfWork.Warehouse.Query()
                .FirstOrDefaultAsync(w => w.Id == warehouseId);

            if (isExisted == null)
            {
                _logger.LogError("Loi warehouse id khong ton tai ham GetListByWarehouseId");
                throw new Exception("Có lỗi xảy ra");
            }

            var list = await _unitOfWork.WarehouseLocation.Query()
                .Where(wl => wl.WarehouseId == warehouseId)
                .ToListAsync();

            return list.Select(wl => new WarehouseLocationList
            {
                Id = wl.Id,
                LocationName= wl.LocationName,
                Status = wl.Status,
            }).ToList();
        }
    }
}
