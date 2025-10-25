using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.Services.Base;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.Warehouse
{
    public class WarehouseService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WarehouseService> logger) : Service(unitOfWork, mapper), IWarehouseService
    {
        private readonly ILogger<WarehouseService> _logger = logger;

        public async Task CreateWarehouseAsync(CreateWarehouse dto)
        {
            var validateName = await _unitOfWork.Warehouse.Query()
                .FirstOrDefaultAsync(w => w.Name == NormalizeName(dto.Name));

            if (validateName != null)
                throw new Exception("Tên kho đã tồn tại");

            var warehouse = new Core.Domain.Entities.Warehouse
            {
                Name = NormalizeName(dto.Name),
                Address = NormalizeName(dto.Address),
                Status = dto.Status,
            };

            await _unitOfWork.Warehouse.AddAsync(warehouse);
            await _unitOfWork.CommitAsync();
        }

        public async Task<List<WarehouseDTO>> GetListWarehouseAsync()
        {
            var list = await _unitOfWork.Warehouse.Query().ToListAsync();

            return _mapper.Map<List<WarehouseDTO>>(list);
        }

        public async Task UpdateWarehouseAsync(UpdateWarehouse dto)
        {
            var isExisted = await _unitOfWork.Warehouse.Query()
                .FirstOrDefaultAsync(w => w.Id == dto.Id);

            if (isExisted == null)
            {
                _logger.LogError("Loi khong tim thay warehouse id ham UpdateWarehouseAsync");
                throw new Exception("Có lỗi xảy ra");
            }

            var validateName = await _unitOfWork.Warehouse.Query()
                .FirstOrDefaultAsync(w => w.Name == NormalizeName(dto.Name) && w.Id != dto.Id);

            if (validateName != null)
                throw new Exception("Tên kho đã tồn tại");

            isExisted.Name = NormalizeName(dto.Name);
            isExisted.Address = NormalizeName(dto.Address);
            isExisted.Status = dto.Status;

            _unitOfWork.Warehouse.Update(isExisted);
            await _unitOfWork.CommitAsync();
        }

        public async Task<WarehouseList> ViewWarehouseDetailsAysnc(int warehouseId)
        {
            var isExisted = await _unitOfWork.Warehouse.Query()
            .Include(w => w.WarehouseLocations)
            .FirstOrDefaultAsync(w => w.Id == warehouseId);

            if (isExisted == null)
            {
                _logger.LogError("Loi khong tim thay warehouse id ham ViewWarehouseDetailsAysnc");
                throw new Exception("Có lỗi xảy ra");
            }

            return new WarehouseList
            {
                Id = isExisted.Id,
                Name = isExisted.Name,
                Address = isExisted.Address,
                Status = isExisted.Status,
                WarehouseLocationLists = isExisted.WarehouseLocations.Select(wl => new DTOs.WarehouseLocation.WarehouseLocationList
                {
                    Id = wl.Id,
                    LocationName = wl.LocationName,
                    Status = wl.Status
                }).ToList()
            };
        }
    }
}
