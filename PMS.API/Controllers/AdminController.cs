
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
        public async Task<IActionResult> Create([FromBody]CreateAccountRequest request)
        {
            try
            {
                await _adminService.CreateAccountAsync(request);
                return Ok("Tạo mới tài khoản thành công");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-account-list")]
        public async Task<IActionResult> List(string? keyword)
        {
            try
            {
                var list = await _adminService.GetAccountListAsync(keyword);
                return Ok(list);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-account-details")]
        public async Task<IActionResult> Detail(string userId)
        {
            try
            {
                var dto = await _adminService.GetAccountDetailAsync(userId);
                return dto == null ? NotFound() : Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update-staff-account")]
        public async Task<IActionResult> Update([FromBody] UpdateAccountRequest request)
        {
            try
            {
                await _adminService.UpdateAccountAsync(request);
                return Ok("Update thành công");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        [HttpPost("suspend-account")]
        public async Task<IActionResult> Suspend(string userId)
        {
            try
            {
                await _adminService.SuspendAccountAsync(userId);
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("active-account")]
        public async Task<IActionResult> Active(string userID)
        {
            try
            {
                await _adminService.ActiveAccountAsync(userID);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }
        }
    }
}
