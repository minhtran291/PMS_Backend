﻿using PMS.Application.DTOs.Warehouse;
using PMS.Core.Domain.Constant;

namespace PMS.Application.Services.Warehouse
{
    public interface IWarehouseService
    {
        Task<ServiceResult<object>> CreateWarehouseAsync(CreateWarehouseDTO dto);
        Task<ServiceResult<object>> UpdateWarehouseAsync(UpdateWarehouseDTO dto);
        Task<ServiceResult<WarehouseDetailsDTO>> ViewWarehouseDetailsAysnc(int warehouseId);
        Task<ServiceResult<List<WarehouseDTO>>> GetListWarehouseAsync();
        Task<ServiceResult<object>> DeleteWarehouseAsync(int warehouseId);
    }
}
