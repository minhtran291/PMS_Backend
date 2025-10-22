using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.PO;
using PMS.Application.Services.PO;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POController : BaseController
    {
        private readonly IPOService _poService;
        public POController(IPOService poService)
        {
            _poService = poService;
        }

        /// <summary>
        /// https://localhost:7213/api/PO/getAllPo
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllPo")]
        [Authorize(Roles = $"{UserRoles.ACCOUNTANT},{UserRoles.PURCHASES_STAFF}")]
        public async Task<IActionResult> GetAllPurchaseOrders()
        {
            var result = await _poService.GetAllPOAsync();
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/PO/updatePo/{id}
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("updatePo/{poid}")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> UpdatePurchaseOrder(int poid, [FromBody] POUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ.", Errors = ModelState });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _poService.UpdatePOAsync(userId, poid, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Lấy chi tiết đơn mua hàng (PO) theo ID
        /// https://localhost:7213/api/PO/GetPoDetailByPoId/poid
        /// </summary>
        [HttpGet("GetPoDetailByPoId/{poid}")]
        [Authorize(Roles = $"{UserRoles.ACCOUNTANT},{UserRoles.PURCHASES_STAFF}")]
        public async Task<IActionResult> GetPurchaseOrderDetail(int poid)
        {
            var result = await _poService.ViewDetailPObyID(poid);
            return HandleServiceResult(result);
        }
    }
}
