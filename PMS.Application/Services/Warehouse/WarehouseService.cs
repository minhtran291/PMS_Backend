using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
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
    }
}
