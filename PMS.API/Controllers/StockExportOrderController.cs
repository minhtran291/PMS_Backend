using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.StockExportOrder;
using PMS.Application.Services.StockExportOrder;
using PMS.Core.Domain.Constant;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockExportOrderController : ControllerBase
    {
        private readonly IStockExportOderService _stockExportOderService;

        public StockExportOrderController(IStockExportOderService service)
        {
            _stockExportOderService = service;
        }

        [HttpPost, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("create-stock-export-order")]
        public async Task<IActionResult> Create(StockExportOrderDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.CreateAsync(dto, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("send-stock-export-order")]
        public async Task<IActionResult> Send(int seoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.SendAsync(seoId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("list-stock-export-order")]
        public async Task<IActionResult> List()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.ListAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("details-stock-export-order")]
        public async Task<IActionResult> Details(int seoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.DetailsAsync(seoId ,userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpPatch, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("update-stock-export-order")]
        public async Task<IActionResult> Update(UpdateStockExportOrderDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.UpdateAsync(dto, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpDelete, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("delete-stock-export-order")]
        public async Task<IActionResult> Delete(int seoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.DeleteAsync(seoId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("stock-export-order-form")]
        public async Task<IActionResult> Form(int soId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.GenerateForm(soId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                Data = result.Data
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("await-stock-export-order")]
        public async Task<IActionResult> AwaitStockExportOrder(int seoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.AwaitStockExportOrder(seoId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("check-ready-to-export")]
        public async Task<IActionResult> CheckReadyToExport (int seoId)
        {
            var result = await _stockExportOderService.CheckAvailable(seoId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("cancel-stock-export-order")]
        public async Task<IActionResult> CancelStockExportOrder(int seoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _stockExportOderService.CancelSEOWithReturn(seoId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.SALES_STAFF + "," + UserRoles.MANAGER)]
        [Route("check-so-have-seo-cancel")]
        public async Task<IActionResult> CheckSoWithSeoCancel(int soId)
        {
            var result = await _stockExportOderService.CheckSOWithSEOCancel(soId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }
    }
}
