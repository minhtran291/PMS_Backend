using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.WarehouseLocation;
using PMS.Core.DTO.WarehouseLocation;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseLocationController : ControllerBase
    {
        private readonly IWarehouseLocationService _warehouseLocationService;

        public WarehouseLocationController(IWarehouseLocationService warehouseLocationService)
        {
            _warehouseLocationService = warehouseLocationService;
        }

        [HttpGet]
        [Route("get-all-warehouse-location")]
        public async Task<IActionResult> WarehouseLocationList()
        {
            try
            {
                var list = await _warehouseLocationService.GetListWarehouseLocation();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("create-warehouse-location")]
        public async Task<IActionResult> CreateWarehouseLocation([FromBody] CreateWarehouseLocation dto)
        {
            try
            {
                await _warehouseLocationService.CreateWarehouseLocation(dto);
                return Ok("Tạo thành công");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("update-warehouse-location")]
        public async Task<IActionResult> UpdateWarehouseLocation([FromBody] UpdateWarehouseLocation dto)
        {
            try
            {
                await _warehouseLocationService.UpdateWarehouseLocation(dto);
                return Ok("Cập nhật thành công");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get-warehouse-location-details")]
        public async Task<IActionResult> WarehouseLocationDetails(int warehouseLocationId)
        {
            try
            {
                var result = await _warehouseLocationService.ViewWarehouseLocationDetails(warehouseLocationId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get-warehouse-location-by-warehouse-id")]
        public async Task<IActionResult> WarehouseLocationListByWarehouseId(int warehouseId)
        {
            try
            {
                var result = await _warehouseLocationService.GetListByWarehouseId(warehouseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
