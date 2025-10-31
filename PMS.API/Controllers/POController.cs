using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.PO;
using PMS.Application.Services.PO;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;

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
        /// https://localhost:7213/api/PO/DepositedPurchaseOrder/{id}
        /// ghi nhận tiền gửi
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("DepositedPurchaseOrder/{poid}")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> DepositedPurchaseOrder(int poid, [FromBody] POUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ.", Errors = ModelState });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _poService.DepositedPOAsync(userId, poid, dto);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// https://localhost:7213/api/PO/DebtAccountantPurchaseOrder/{id}
        /// ghi nhận thanh toán
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("DebtAccountantPurchaseOrder/{poid}")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> DebtAccountantPurchaseOrder(int poid, [FromBody] POUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ.", Errors = ModelState });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _poService.DebtAccountantPOAsync(userId, poid, dto);
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


        /// <summary>
        /// https://localhost:7213/api/PO/{poid}/status?newStatus=approved
        /// </summary>
        /// <param name="poid"></param>
        /// <param name="newStatus"></param>
        /// <returns></returns>
        [HttpPut("{poid}/status")]
        [Authorize(Roles = $"{UserRoles.ACCOUNTANT},{UserRoles.PURCHASES_STAFF}")]
        public async Task<IActionResult> ChangeStatus(int poid, [FromQuery] PurchasingOrderStatus newStatus)
        {
            var result = await _poService.ChangeStatusAsync(poid, newStatus);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/PO/exportPayment/{poid}
        /// </summary>
        /// <param name="poid"></param>
        /// <returns></returns>
        [HttpGet("exportPayment/{poid}")]
        public async Task<IActionResult> ExportPOPaymentExcel(int poid)
        {
            var excelBytes = await _poService.GeneratePOPaymentExcelAsync(poid);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PO_{poid}_Payment.xlsx");
        }
    }
}
