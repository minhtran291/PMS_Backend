using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.Warehouse;
using PMS.Core.DTO.Warehouse;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpGet]
        [Route("get-all-warehouse")]
        public async Task<IActionResult> WarehouseList()
        {
            try
            {
                var list = await _warehouseService.GetListWarehouseAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("create-warehouse")]
        public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouse dto)
        {
            try
            {
                await _warehouseService.CreateWarehouseAsync(dto);
                return Ok("Tạo thành công");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("update-warehouse")]
        public async Task<IActionResult> UpdateWarehouse([FromBody] UpdateWarehouse dto)
        {
            try
            {
                await _warehouseService.UpdateWarehouseAsync(dto);
                return Ok("Cập nhật thành công");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get-warehouse-details")]
        public async Task<IActionResult> WarehouseDetails(int warehouseId)
        {
            try
            {
                var result = await _warehouseService.ViewWarehouseDetailsAysnc(warehouseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
