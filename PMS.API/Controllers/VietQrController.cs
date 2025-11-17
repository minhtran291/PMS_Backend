using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.VietQR;
using PMS.Application.Services.VietQR;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VietQrController : ControllerBase
    {
        private readonly IVietQrService _service;
        public VietQrController(IVietQrService service) => _service = service;

        //[HttpPost("init")]
        //[ProducesResponseType(typeof(ServiceResult<VietQrInitResponse>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> Init([FromBody] VietQrInitRequest req)
        //{
        //    var result = await _service.InitAsync(req);
        //    return StatusCode(result.StatusCode, new
        //    {
        //        success = result.Success,
        //        message = result.Message,
        //        data = result.Data
        //    });
        //}

        //[HttpPost("confirm")]
        //[ProducesResponseType(typeof(ServiceResult<bool>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> Confirm([FromBody] VietQrConfirmRequest req)
        //{
        //    var result = await _service.ConfirmAsync(req);
        //    return StatusCode(result.StatusCode, new
        //    {
        //        success = result.Success,
        //        message = result.Message,
        //        data = result.Data
        //    });
        //}
    }
}
