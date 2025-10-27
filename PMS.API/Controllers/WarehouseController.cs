using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Warehouse;
using PMS.Application.DTOs.Warehouse;

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
        /// <summary>
        /// https://localhost:7213/api/Warehouse/get-all-warehouse
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("get-all-warehouse")]
        public async Task<IActionResult> WarehouseList()
        {
            var result = await _warehouseService.GetListWarehouseAsync();
            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost]
        [Route("create-warehouse")]
        public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseDTO dto)
        {
            var result = await _warehouseService.CreateWarehouseAsync(dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpPut]
        [Route("update-warehouse")]
        public async Task<IActionResult> UpdateWarehouse([FromBody] UpdateWarehouseDTO dto)
        {
            var result = await _warehouseService.UpdateWarehouseAsync(dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }
        /// <summary>
        /// https://localhost:7213/api/Warehouse/get-warehouse-details/{}
        /// </summary>
        /// <param name="warehouseId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("get-warehouse-details/{warehouseId}")]
        public async Task<IActionResult> WarehouseDetails(int warehouseId)
        {
            var result = await _warehouseService.ViewWarehouseDetailsAysnc(warehouseId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpDelete]
        [Route("delete-warehouse")]
        public async Task<IActionResult> DeleteWarehouse(int warehouseId)
        {
            var result = await _warehouseService.DeleteWarehouseAsync(warehouseId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }
    }
}
