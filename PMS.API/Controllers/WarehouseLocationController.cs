using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.WarehouseLocation;
using PMS.Application.DTOs.WarehouseLocation;
using Microsoft.AspNetCore.Authorization;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseLocationController : BaseController
    {
        private readonly IWarehouseLocationService _warehouseLocationService;

        public WarehouseLocationController(IWarehouseLocationService warehouseLocationService)
        {
            _warehouseLocationService = warehouseLocationService;
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        [Route("get-all-warehouse-location")]
        public async Task<IActionResult> WarehouseLocationList()
        {
            var result = await _warehouseLocationService.GetListWarehouseLocation();

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// https://localhost:7213/api/WarehouseLocation/create-warehouse-location
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        [Route("create-warehouse-location")]
        public async Task<IActionResult> CreateWarehouseLocation([FromBody] CreateWarehouseLocationDTO dto)
        {
            var result = await _warehouseLocationService.CreateWarehouseLocation(dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        /// <summary>
        /// https://localhost:7213/api/WarehouseLocation/update-warehouse-location
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        [Route("update-warehouse-location")]
        public async Task<IActionResult> UpdateWarehouseLocation([FromBody] UpdateWarehouseLocationDTO dto)
        {
            var result = await _warehouseLocationService.UpdateWarehouseLocation(dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }
        /// <summary>
        /// https://localhost:7213/api/WarehouseLocation/get-warehouse-location-details/{}
        /// </summary>
        /// <param name="warehouseLocationId"></param>
        /// <returns></returns>
        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.PURCHASES_STAFF)]
        [Route("get-warehouse-location-details/{warehouseLocationId}")]
        public async Task<IActionResult> WarehouseLocationDetails(int warehouseLocationId)
        {
            var result = await _warehouseLocationService.ViewWarehouseLocationDetails(warehouseLocationId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        [Route("get-warehouse-location-by-warehouse-id")]
        public async Task<IActionResult> WarehouseLocationListByWarehouseId(int warehouseId)
        {
            var result = await _warehouseLocationService.GetListByWarehouseId(warehouseId);
            
            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpDelete, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        [Route("delete")]
        public async Task<IActionResult> DeleteWarehouseLocation(int warehouseLocationId)
        {
            var result = await _warehouseLocationService.DeleteWarehouseLocation(warehouseLocationId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }
    }
}
