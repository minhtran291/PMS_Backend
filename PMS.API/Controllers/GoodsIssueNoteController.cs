using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.GoodsIssueNote;
using PMS.Application.DTOs.StockExportOrder;
using PMS.Application.Services.GoodsIssueNote;
using PMS.Core.Domain.Constant;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GoodsIssueNoteController : ControllerBase
    {
        private readonly IGoodsIssueNoteService _goodsIssueNoteService;

        public GoodsIssueNoteController(IGoodsIssueNoteService service)
        {
            _goodsIssueNoteService = service;
        }

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.ACCOUNTANT)]
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

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.ACCOUNTANT)]
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

        [HttpPatch, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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

        [HttpDelete, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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

        [HttpGet, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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

        [HttpPost, Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        [Route("response-not-enough")]
        public async Task<IActionResult> NotEnough(int stockExportOrder)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không chứa thông tin định danh người dùng");

            var result = await _goodsIssueNoteService.ResponseNotEnough(stockExportOrder, userId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }
    }
}
