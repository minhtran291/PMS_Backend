
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.Admin;
using PMS.Core.Domain.Constant;
using PMS.Core.DTO.Admin;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase

    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost("create-staff-account")]
        public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminService.CreateAccountAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("get-account-list")]
        public async Task<IActionResult> List(string? keyword)
        {
            try
            {
                var list = await _adminService.GetAccountListAsync(keyword);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-account-details")]
        public async Task<IActionResult> Detail(string userId)
        {
            var result = await _adminService.GetAccountDetailAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPut("update-staff-account")]
        public async Task<IActionResult> Update([FromBody] UpdateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _adminService.UpdateAccountAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost("suspend-account")]
        public async Task<IActionResult> Suspend(string userId)
        {
            var result = await _adminService.SuspendAccountAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost("active-account")]
        public async Task<IActionResult> Active(string userID)
        {
            var result = await _adminService.ActiveAccountAsync(userID);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
    }
}
