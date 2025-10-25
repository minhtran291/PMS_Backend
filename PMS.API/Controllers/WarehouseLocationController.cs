using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.WarehouseLocation;
using PMS.Application.DTOs.WarehouseLocation;

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

        [HttpGet]
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
        [HttpPost]
        [Route("create-warehouse-location")]
        public async Task<IActionResult> CreateWarehouseLocation([FromBody] CreateWarehouseLocationDTO dto)
        {
            var result =  await _warehouseLocationService.CreateWarehouseLocation(dto);

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
        [HttpPut]
        [Route("update-warehouse-location")]
        public async Task<IActionResult> UpdateWarehouseLocation([FromBody] UpdateWarehouseLocation dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
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
        /// <summary>
        /// https://localhost:7213/api/WarehouseLocation/get-warehouse-location-details/{}
        /// </summary>
        /// <param name="warehouseLocationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("get-warehouse-location-details/{warehouseLocationId}")]
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

        /// <summary>
        /// https://localhost:7213/api/WarehouseLocation/storeLot
        /// Gán lô hàng (Lot) vào vị trí trong kho
        /// </summary>
        //[HttpPut("storeLot")]
        //public async Task<IActionResult> StoreLotInWarehouse([FromBody] StoringLot dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var result = await _warehouseLocationService.StoringLotInWarehouseLocation(dto);
        //    return HandleServiceResult(result);
        //}
    }
}
