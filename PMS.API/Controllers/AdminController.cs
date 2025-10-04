using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.Admin;
using PMS.Core.DTO.Admin;

namespace PMS.API.Controllers
{
    [ApiController]
    [Route("api/admin/accounts")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> Create(AdminCreateAccountRequest req, CancellationToken ct)
        => Ok(await _adminService.CreateAccountAsync(req, ct));

        [HttpGet("allAccounts")]
        public async Task<ActionResult<List<AdminAccountListItem>>> List([FromQuery] string? q, CancellationToken ct)
            => Ok(await _adminService.GetAccountsAsync(q, ct));

        [HttpGet("{userId}/account_details")]
        public async Task<ActionResult<AdminAccountDetail>> Detail(string userId, CancellationToken ct)
        {
            var dto = await _adminService.GetAccountDetailAsync(userId, ct);
            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpPut("{userId}/update")]
        public async Task<IActionResult> Update([FromRoute] string userId, AdminUpdateAccountRequest req, CancellationToken ct)
        {
            await _adminService.UpdateAccountAsync(userId, req, ct);
            return NoContent();
        }

        [HttpPost("{userId}/suspend")]
        public async Task<IActionResult> Suspend(string userId, CancellationToken ct)
        {
            await _adminService.SuspendAccountAsync(userId, ct);
            return NoContent();
        }
    }
}
