using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Supplier;
using PMS.Application.Services.Supplier;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _service;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(ISupplierService service, ILogger<SupplierController> logger)
        {
            _service = service; _logger = logger;
        }

        [HttpPost("create")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> CreateSupplierAsync([FromBody] CreateSupplierRequestDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return StatusCode(400, new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    data = ModelState
                });
            }

            var result = await _service.CreateAsync(dto);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });

        }

        [HttpGet("list"), Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetSupplierListAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
        {
            var result = await _service.GetPagedAsync(page, pageSize, keyword);

            return StatusCode(result.StatusCode, new
            {
                success = result.StatusCode == 200,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("detail")]
       // [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.SALES_STAFF)]
        public async Task<IActionResult> GetSupplierByIdAsync([FromQuery] int id)
        {
            var result = await _service.GetByIdAsync(id);

            return StatusCode(result.StatusCode, new
            {
                success = result.StatusCode == 200,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPut("update")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> UpdateSupplierAsync([FromQuery] int id, [FromBody] UpdateSupplierRequestDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return StatusCode(400, new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    data = ModelState
                });
            }

            var result = await _service.UpdateAsync(id, dto);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost("enable")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> EnableSupplierAsync([FromQuery] string supplierId)
        {
            var result = await _service.EnableSupplier(supplierId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost("disable")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> DisableSupplierAsync([FromQuery] string supplierId)
        {
            var result = await _service.DisableSupplier(supplierId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// Lấy danh sách sản phẩm (theo từng lot) của nhà cung cấp
        /// http://localhost:5137/api/Supplier/BySupplier/{supplierId}
        /// </summary>
        /// <param name="supplierId">ID nhà cung cấp</param>
        /// <returns>Danh sách LotProduct theo Supplier</returns>
        [HttpGet("BySupplier/{supplierId}")]
        public async Task<IActionResult> GetProductsBySupplier(string supplierId)
        {
            var result = await _service.ListProductBySupId(supplierId);

            if (result.Equals(0))
                return BadRequest(result);

            return Ok(result);
        }
    }
}
