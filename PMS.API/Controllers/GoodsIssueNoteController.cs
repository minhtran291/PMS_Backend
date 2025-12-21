using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.GoodsIssueNote;
using PMS.Application.DTOs.StockExportOrder;
using PMS.Application.Services.GoodsIssueNote;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Constant;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GoodsIssueNoteController : ControllerBase
    {
        private readonly IGoodsIssueNoteService _goodsIssueNoteService;
        private readonly ISalesOrderService _salesOrder;

        public GoodsIssueNoteController(IGoodsIssueNoteService service, ISalesOrderService salesOrderService)
        {
            _goodsIssueNoteService = service;
            _salesOrder = salesOrderService;
        }

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("create-goods-issue-note")]
        public async Task<IActionResult> Create(CreateGoodsIssueNoteDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.CreateAsync(dto, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("send-goods-issue-note")]
        public async Task<IActionResult> Send(int ginId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.SendAsync(ginId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        [Route("goods-issue-note-list")]
        public async Task<IActionResult> List()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.ListAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        [Route("goods-issue-note-details")]
        public async Task<IActionResult> Details(int ginId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.DetailsAsync(ginId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpPatch, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("update-goods-issue-note")]
        public async Task<IActionResult> Update(UpdateGoodsIssueNoteDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.UpdateAsync(dto, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpDelete, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("delete-goods-issue-note")]
        public async Task<IActionResult> Delete(int ginId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.DeleteAsync(ginId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("warnings")]
        public async Task<IActionResult> Warnings()
        {
            var result = await _goodsIssueNoteService.WarningAsync();

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("response-not-enough")]
        public async Task<IActionResult> NotEnough(int stockExportOrderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.ResponseNotEnough(stockExportOrderId, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("exported-lot-product")]
        public async Task<IActionResult> ExportedLotProduct(int goodsIssueNoteId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.ExportLotProduct(goodsIssueNoteId, userId);

            if (result.Success)
            {
                await _salesOrder.CheckAndUpdateDeliveredStatusAsync();
            }

            if(result.StatusCode == 200)
            {
                await _goodsIssueNoteService.CheckQuantity(userId);
            }

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("exported-statistic")]
        public async Task<IActionResult> Statistic()
        {
            var result = await _goodsIssueNoteService.StatisticAsync();

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
        [Route("not-exported-statistic")]
        public async Task<IActionResult> NotExported()
        {
            var result = await _goodsIssueNoteService.NotExportedAsync();

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        [Route("download-goods-issue-note")]
        public async Task<IActionResult> Download(int ginId)
        {
            var result = await _goodsIssueNoteService.DownloadGIN(ginId);

            if (!result.Success)
                return StatusCode(result.StatusCode, new
                {
                    message = result.Message
                });

            var fileName = $"PhieuXuatKho.pdf";

            return File(
                result.Data!,
                "application/pdf",
                fileName);
        }
    }
}
