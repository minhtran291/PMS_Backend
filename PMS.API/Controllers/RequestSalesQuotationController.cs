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
        [Route("create-request")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRsqDTO dto)
        {
            var customerId = User.FindFirstValue("customer_id");

            var result = await _requestSalesQuotationService.CreateRequestSalesQuotation(dto, customerId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.SALES_STAFF)]
        [Route("view-list")]
        public async Task<IActionResult> ViewRequestList()
        {
            var customerId = User.FindFirstValue("customer_id");

            var staffId = User.FindFirstValue("staff_id");

            var result = await _requestSalesQuotationService.ViewRequestSalesQuotationList(customerId, staffId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.SALES_STAFF)]
        [Route("view-details")]
        public async Task<IActionResult> ViewRequestDetails(int rsqId)
        {
            var customerId = User.FindFirstValue("customer_id");

            var staffId = User.FindFirstValue("staff_id");

            var result = await _requestSalesQuotationService.ViewRequestSalesQuotationDetails(rsqId, customerId, staffId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpPut, Authorize(Roles = UserRoles.CUSTOMER)]
        [Route("update-request")]
        public async Task<IActionResult> UpdateRequest([FromBody]UpdateRsqDTO dto)
        {
            var customerId = User.FindFirstValue("customer_id");

            var result = await _requestSalesQuotationService.UpdateRequestSalesQuotation(dto, customerId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.CUSTOMER)]
        [Route("send-request")]
        public async Task<IActionResult> SendRequest(int rsqId)
        {
            var customerId = User.FindFirstValue("customer_id");

            var result = await _requestSalesQuotationService.SendSalesQuotationRequest(customerId, rsqId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpDelete, Authorize(Roles = UserRoles.CUSTOMER)]
        [Route("delete-request")]
        public async Task<IActionResult> DeleteRequest(int rsqId)
        {
            var customerId = User.FindFirstValue("customer_id");

            var result = await _requestSalesQuotationService.RemoveRequestSalesQuotation(rsqId, customerId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }
    }
}
