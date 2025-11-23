using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.Product;
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
        [Authorize(Roles = $"{UserRoles.PURCHASES_STAFF},{UserRoles.WAREHOUSE_STAFF}")]
        public async Task<IActionResult> UpdateSalePrice(int whlcid, int lotid, [FromBody] decimal newSalePrice)
        {
            var result = await _warehouseService.UpdateSalePriceAsync(whlcid, lotid, newSalePrice);
            return HandleServiceResult(result);
        }



        /// <summary>
        /// https://localhost:7213/api/Warehouse/create-session/{whlcid}
        /// 1️⃣ Tạo phiên kiểm kê mới (tạo InventorySession + InventoryHistory cho từng Lot)
        /// </summary>
        [HttpPost("create-session/{whlcid}")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> CreateInventorySession(int whlcid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            var result = await _warehouseService.CreateInventorySessionAsync(userId, whlcid);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/Warehouse/update-count
        /// Cập nhật số lượng thực tế của Lot trong phiên kiểm kê
        /// </summary>
        [HttpPut("update-count")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> UpdateInventoryBatch([FromBody] UpdateInventoryBatchDto input)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            var result = await _warehouseService.UpdateInventoryBatchAsync(userId,input);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/Warehouse/comparison/{sessionId}
        /// Lấy danh sách so sánh chênh lệch giữa thực tế và hệ thống của phiên kiểm kê
        /// </summary>
        [HttpGet("comparison/{sessionId}")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> GetInventoryComparison(int sessionId)
        {
            var result = await _warehouseService.GetInventoryComparisonAsync(sessionId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/Warehouse/complete-session/{sessionId}
        /// 4️⃣ Hoàn tất phiên kiểm kê (cập nhật tồn kho thực tế)
        /// </summary>
        [HttpPost("complete-session/{sessionId}")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> CompleteInventorySession(int sessionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            var result = await _warehouseService.CompleteInventorySessionAsync(sessionId, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/Warehouse/session/{sessionId}/histories
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        [HttpGet("session/{sessionId}/histories")]
        //[Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> GetHistoriesBySessionId(int sessionId)
        {
            var result = await _warehouseService.GetHistoriesBySessionIdAsync(sessionId);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// Xuất Excel toàn bộ InventoryHistories của một phiên kiểm kê
        /// https://localhost:7213/api/Warehouse/session/{sessionId}/export
        /// </summary>
        /// <param name="sessionId">ID phiên kiểm kê</param>
        [HttpGet("session/{sessionId}/export")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> ExportInventorySessionToExcel(int sessionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            var result = await _warehouseService.ExportInventorySessionToExcelAsync(userId, sessionId);

            if (!result.Success)
                return HandleServiceResult(result);

            var fileName = $"BaoCaoKiemKeTheoPhien_{sessionId}_Author_{userId}{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        /// <summary>
        /// https://localhost:7213/api/Warehouse/GetAllsession
        /// Lấy danh sách tất cả các phiên kiểm kê 
        /// </summary>
        [HttpGet("GetAllsession")]
        public async Task<IActionResult> GetAllInventorySessionsAsync()
        {
            var result = await _warehouseService.GetAllInventorySessionsAsync();
            return HandleServiceResult(result);
        }



        /// <summary>
        /// https://localhost:7213/api/Warehouse/sessionbywarehouse/{warehouseLocationId}
        /// </summary>
        /// <param name="warehouseLocationId"></param>
        /// <returns></returns>
        [HttpGet("sessionbywarehouse/{warehouseLocationId}")]
        public async Task<IActionResult> GetAllSessionsByWarehouse(int warehouseLocationId)
        {
            var result = await _warehouseService.GetAllInventorySessionsByWarehouseLocationAsync(warehouseLocationId);
            return HandleServiceResult(result);
        }

    }
}
