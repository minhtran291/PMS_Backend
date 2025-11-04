using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.DTOs.WarehouseLocation;
using PMS.Application.Services.Warehouse;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : BaseController
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
        //[Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
       // [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> DeleteWarehouse(int warehouseId)
        {
            var result = await _warehouseService.DeleteWarehouseAsync(warehouseId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }


        /// <summary>
        /// Lấy tất cả sản phẩm theo vị trí trong kho (Warehouse Location ID)
        /// https://localhost:7213/api/Warehouse/warehouse-location/{whlcid}
        /// </summary>
        /// <param name="whlcid">ID vị trí trong kho</param>
        [HttpGet("warehouse-location/{whlcid}")]
        // [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> GetAllLotByWHLID(int whlcid)
        {
            try
            {
                var result = await _warehouseService.GetAllLotByWHLID(whlcid);
                return HandleServiceResult(result); 
            }
            catch (Exception ex)
            {
                

                var errorResult = new ServiceResult<List<LotProductDTO>>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "Đã xảy ra lỗi hệ thống khi xử lý dữ liệu."
                };

                return HandleServiceResult(errorResult);
            }
        }

        /// <summary>
        /// https://localhost:7213/api/Warehouse/warehouse-location/{whlcid}/product/{lotid}/update-saleprice
        /// </summary>
        /// <param name="whlcid"></param>
        /// <param name="lotid"></param>
        /// <param name="newSalePrice"></param>
        /// <returns></returns>
        [HttpPut("warehouse-location/{whlcid}/lot/{lotid}/update-saleprice")]
        // [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> UpdateSalePrice(int whlcid, int lotid, [FromBody] decimal newSalePrice)
        {
            var result = await _warehouseService.UpdateSalePriceAsync(whlcid, lotid, newSalePrice);
            return HandleServiceResult(result);
        }

    }
}
