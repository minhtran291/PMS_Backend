using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Services.Base;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Data.UnitOfWork;

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
                RowNo = dto.RowNo,
                ColumnNo = dto.ColumnNo,
                LevelNo = dto.LevelNo,
                Status = Core.Domain.Enums.WarehouseLocationStatus.Active
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
                RowNo = w.RowNo,
                ColumnNo = w.ColumnNo,
                LevelNo = w.LevelNo,
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

            isExisted.RowNo = dto.RowNo;
            isExisted.ColumnNo = dto.ColumnNo;
            isExisted.LevelNo = dto.LevelNo;
            isExisted.Status = dto.Status;

            _unitOfWork.WarehouseLocation.Update(isExisted);
            await _unitOfWork.CommitAsync();
        }

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
                RowNo = isExisted.RowNo,
                ColumnNo = isExisted.ColumnNo,
                LevelNo = isExisted.LevelNo,
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
                RowNo = wl.RowNo,
                ColumnNo = wl.ColumnNo,
                LevelNo = wl.LevelNo,
                Status = wl.Status,
            }).ToList();
        }
    }
}
