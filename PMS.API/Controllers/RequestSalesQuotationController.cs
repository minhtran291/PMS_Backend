using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Application.Services.RequestSalesQuotation;
using PMS.Core.Domain.Constant;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestSalesQuotationController : ControllerBase
    {
        private readonly IRequestSalesQuotationService _requestSalesQuotationService;

        public RequestSalesQuotationController(IRequestSalesQuotationService requestSalesQuotationService)
        {
            _requestSalesQuotationService = requestSalesQuotationService;
        }

        [HttpPost, Authorize(Roles = UserRoles.CUSTOMER)]
        [Route("create")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRsqDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _requestSalesQuotationService.CreateRequestSalesQuotation(dto, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }
    }
}
