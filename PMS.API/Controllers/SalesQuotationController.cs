using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.Services.SalesQuotation;
using PMS.Core.Domain.Constant;
using StackExchange.Redis;
using System.Security.Claims;

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

        [HttpPost, Authorize(Roles = UserRoles.SALES_STAFF)]
        [Route("create-sales-quotation")]
        public async Task<IActionResult> CreateSalesQuotation([FromBody] CreateSalesQuotationDTO dto)
        {
            var salesStaffId = User.FindFirstValue("staff_id");

            if (string.IsNullOrEmpty(salesStaffId))
                return Unauthorized();

            var result = await _salesQuotationService.CreateSalesQuotationAsync(dto, salesStaffId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPatch, Authorize(Roles = UserRoles.SALES_STAFF)]
        [Route("update-sales-quotation")]
        public async Task<IActionResult> UpdateSalesQuotation([FromBody] UpdateSalesQuotationDTO dto)
        {
            var salesStaffId = User.FindFirstValue("staff_id");

            if (string.IsNullOrEmpty(salesStaffId))
                return Unauthorized();

            var result = await _salesQuotationService.UpdateSalesQuotationAsync(dto, salesStaffId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpDelete, Authorize(Roles = UserRoles.SALES_STAFF)]
        [Route("delete-sales-quotation")]
        public async Task<IActionResult> DeleteSalesQuotation(int sqId)
        {
            var salesStaffId = User.FindFirstValue("staff_id");

            if (string.IsNullOrEmpty(salesStaffId))
                return Unauthorized();

            var result = await _salesQuotationService.DeleteSalesQuotationAsync(sqId, salesStaffId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.CUSTOMER)]
        [Route("view-list")]
        public async Task<IActionResult> SalesQuotationList()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var salesStaffId = User.FindFirstValue("staff_id");

            if (string.IsNullOrEmpty(salesStaffId) || string.IsNullOrEmpty(role))
                return Unauthorized();

            var result = await _salesQuotationService.SalesQuotationListAsync(role, salesStaffId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.SALES_STAFF)]
        [Route("send-sales-quotation")]
        public async Task<IActionResult> SendSalesQuotation(int sqId)
        {
            var salesStaffId = User.FindFirstValue("staff_id");

            if (string.IsNullOrEmpty(salesStaffId))
                return Unauthorized();

            var result = await _salesQuotationService.SendSalesQuotationAsync(sqId, salesStaffId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.SALES_STAFF)]
        [Route("add-sales-quotation-comment")]
        public async Task<IActionResult> AddComment(AddSalesQuotationCommentDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _salesQuotationService.AddSalesQuotationComment(dto, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.SALES_STAFF)]
        [Route("view-sales-quotation-details")]
        public async Task<IActionResult> ViewSalesQuotationDetails(int sqId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _salesQuotationService.SalesQuotaionDetailsAsync(sqId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
    }
}
