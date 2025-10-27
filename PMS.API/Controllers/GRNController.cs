using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.GRNService;
using PMS.Application.DTOs.GRN;
using PMS.Core.DTO.Content;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GRNController : BaseController
    {
        private readonly IGRNService _IGRNService;
        private readonly ILogger<GRNController> _logger;
        public GRNController(IGRNService IGRNService, ILogger<GRNController> logger)
        {
            _IGRNService = IGRNService;
            _logger = logger;
        }

        /// <summary>
        /// https://localhost:7213/api/GRN/createGRNFromPo/{poid}
        /// </summary>
        /// <param name="poid"></param>
        /// <returns></returns>
        [HttpPost("createGRNFromPo/{poId:int}")]
        public async Task<IActionResult> CreateGoodReceiptNoteFromPO(int poId, CreateGrnFromPoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Dữ liệu gửi lên không hợp lệ.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            try
            {
                var result = await _IGRNService.CreateGoodReceiptNoteFromPOAsync(userId, poId, dto.WarehouseLocationID);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo GRN từ PO ID: {poId}", poId);
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        /// <summary>
        /// Tạo phiếu nhập kho thủ công (Manual GRN) cho đơn mua hàng (PO)
        /// https://localhost:7213/api/GRN/CreateGRNManually/{poid}
        /// </summary>
        /// <param name="poId">ID của đơn mua hàng</param>
        /// <param name="dto">Thông tin phiếu nhập kho</param>
        [HttpPost("CreateGRNManually/{poId}")]
        public async Task<IActionResult> CreateGRNByManually(int poId, [FromBody] GRNManuallyDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Dữ liệu gửi lên không hợp lệ.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });
            }
            var result = await _IGRNService.CreateGRNByManually(userId, poId, dto);

            return HandleServiceResult(result);
        }
    }
}
