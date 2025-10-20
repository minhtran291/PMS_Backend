using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.PRFQService;
using PMS.Application.DTOs.PRFQ;
using PMS.Application.DTOs.RequestSalesQuotation;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PRFQController : BaseController
    {
        private readonly IPRFQService _iPRFQService;

        public PRFQController(IPRFQService iPRFQService)
        {
            _iPRFQService = iPRFQService;
        }

        /// <summary>
        /// https://localhost:7213/api/PRFQ/quotationforsupplier
        /// send an email with products quotation request for supplier by personal email config in database
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        /// 
        [HttpPost("quotationforsupplier")]
        //[Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> CreatePRFQ([FromBody] CreatePRFQDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _iPRFQService.CreatePRFQAsync(
                userId,
                dto.SupplierId,
                dto.TaxCode,
                dto.MyPhone,
                dto.MyAddress,
                dto.ProductIds
            );

            return HandleServiceResult(result);
        }
    }
}
