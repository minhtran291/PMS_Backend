using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.SalesQuotation;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesQuotationController : ControllerBase
    {
        private readonly ISalesQuotationService _salesQuotationService;

        public SalesQuotationController(ISalesQuotationService salesQuotationService)
        {
            _salesQuotationService = salesQuotationService;
        }

        [HttpGet, Authorize(Roles = UserRoles.SALES_STAFF)]
        [Route("generate-form")]
        public async Task<IActionResult> GenerateForm(int rsqId)
        {
            var result = await _salesQuotationService.GenerateFormAsync(rsqId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
    }
}
