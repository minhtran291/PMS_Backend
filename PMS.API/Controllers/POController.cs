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
        [Authorize(Roles = $"{UserRoles.ACCOUNTANT},{UserRoles.PURCHASES_STAFF},{UserRoles.WAREHOUSE_STAFF}")]
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
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> ChangeStatus(int poid, [FromQuery] PurchasingOrderStatus newStatus)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ.", Errors = ModelState });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            var result = await _poService.ChangeStatusAsync(userId,poid, newStatus);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// Xuất file PDF báo cáo thanh toán đơn hàng (PO Payment Report)
        /// GET: https://localhost:7213/api/PO/exportPaymentPdf/{poid}
        /// </summary>
        /// <param name="poid">Mã PO cần xuất</param>
        /// <returns>File PDF</returns>
        [HttpGet("exportPaymentPdf/{poid}")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> ExportPOPaymentPdf(int poid)
        {
            var pdfBytes = await _poService.GeneratePOPaymentPdfAsync(poid);
            return File(pdfBytes, "application/pdf", $"PO_{poid}_Payment.pdf");
        }



        /// <summary>
        /// https://localhost:7213/api/PO/by-receiving-status
        /// Lấy tất cả PO được phân loại theo trạng thái nhập kho (đủ, một phần, chưa nhập)
        /// </summary>
        [HttpGet("by-receiving-status")]
        public async Task<IActionResult> GetPOByReceivingStatusAsync()
        {
            var result = await _poService.GetPOByReceivingStatusAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// https://localhost:7213/api/PO/fully-received
        /// Lấy danh sách PO đã nhập đủ hàng
        /// </summary>
        [HttpGet("fully-received")]
        public async Task<IActionResult> GetFullyReceivedAsync()
        {
            var result = await _poService.GetPOByReceivingStatusAsync();
            return Ok(result.Data["FullyReceived"]);
        }

        /// <summary>
        /// https://localhost:7213/api/PO/partially-received
        /// Lấy danh sách PO mới nhập một phần hàng
        /// </summary>
        [HttpGet("partially-received")]
        public async Task<IActionResult> GetPartiallyReceivedAsync()
        {
            var result = await _poService.GetPOByReceivingStatusAsync();
            return Ok(result.Data["PartiallyReceived"]);
        }

        /// <summary>
        /// https://localhost:7213/api/PO/not-received
        /// Lấy danh sách PO chưa nhập hàng nào
        /// </summary>
        [HttpGet("not-received")]
        public async Task<IActionResult> GetNotReceivedAsync()
        {
            var result = await _poService.GetPOByReceivingStatusAsync();
            return Ok(result.Data["NotReceived"]);
        }


        /// <summary>
        /// Xóa PO có trạng thái "draft"
        /// https://localhost:7213/api/PO/deletePOWithDraftStatus/{poid}
        /// </summary>
        /// <param name="poid">ID của PO cần xóa</param>
        /// <returns>Trả về kết quả xóa</returns>
        [HttpDelete("deletePOWithDraftStatus/{poid}")]
        public async Task<IActionResult> DeletePOWithDraftStatus(int poid)
        {
            var result = await _poService.DeletePOWithDraftStatus(poid);
            return HandleServiceResult(result);
        }
    }
}
