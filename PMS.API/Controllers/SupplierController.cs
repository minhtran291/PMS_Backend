using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Supplier;
using PMS.Core.Domain.Constant;
using PMS.Application.DTOs.Supplier;

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
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> Create([FromBody] CreateSupplierRequestDTO dto)
        {
            try
            {
                if (!ModelState.IsValid) return ValidationProblem(ModelState);
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create supplier failed");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("list")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
        {
            try
            {
                var list = await _service.GetPagedAsync(page, pageSize, keyword);
                if (list.Count == 0) return NotFound("Không có dữ liệu");
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("detail")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> GetById([FromQuery] int id)
        {
            try
            {
                var dto = await _service.GetByIdAsync(id);
                return dto == null ? NotFound() : Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> Update([FromQuery] int id, [FromBody] UpdateSupplierRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                if (!ModelState.IsValid) return ValidationProblem(ModelState);
                var result = await _service.UpdateAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("enable")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> Enable([FromQuery] string supplierId)
        {
            try
            {
                await _service.EnableSupplier(supplierId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("disable")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> Disable([FromQuery] string supplierId)
        {
            try
            {
                await _service.DisableSupplier(supplierId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
