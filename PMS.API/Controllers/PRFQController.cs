using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.PRFQService;
using PMS.Application.DTOs.PRFQ;
using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PRFQController : BaseController
    {
        private readonly IPRFQService _iPRFQService;

        public PRFQController(IPRFQService iPRFQService)
        {
            _iPRFQService = iPRFQService;
        }

        /// <summary>
        /// https://localhost:7213/api/PRFQ/quotationforsupplier
        /// send an email with products quotation request for supplier by personal email config in database
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        /// 
        [HttpPost("quotationforsupplier")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> CreatePRFQ([FromBody] CreatePRFQDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });

            var result = await _iPRFQService.CreatePRFQAsync(
                userId,
                dto.SupplierId,
                dto.TaxCode,
                dto.MyPhone,
                dto.MyAddress,
                dto.ProductIds,
                dto.PRFQStatus
            );

            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST https://localhost:7213/api/PRFQ/convertToPo
        /// Chuyển báo giá Excel (đã preview trước đó) thành đơn hàng chính thức (Purchase Order)
        /// và gửi mail xác nhận lại cho supplier.
        /// </summary>
        /// <param name="input">ExcelKey (được sinh khi preview) + danh sách sản phẩm có Quantity</param>
        /// <returns>POID và thông báo kết quả</returns>
        [HttpPost("convertToPo")]
        [Consumes("application/json")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> ConvertToPurchaseOrder([FromBody] PurchaseOrderInputDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ServiceResult<string>
                {
                    StatusCode = 400,
                    Message = "Dữ liệu đầu vào không hợp lệ.",
                    Data = null
                });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return HandleServiceResult(new ServiceResult<string>
                    {
                        StatusCode = 401,
                        Message = "Không thể xác thực người dùng."
                    });
                }

                var result = await _iPRFQService.ConvertExcelToPurchaseOrderAsync(userId, input);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleServiceResult(new ServiceResult<string>
                {
                    StatusCode = 500,
                    Message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }


        /// <summary>
        /// https://localhost:7213/api/PRFQ/previewExcel
        /// Xem trước danh sách sản phẩm trong file báo giá Excel của supplier.
        /// </summary>
        /// <param name="excelFile">File Excel do supplier gửi</param>
        /// <returns>Danh sách sản phẩm gồm ProductID, Mô tả, ĐVT, Giá báo...</returns>
        [HttpPost("previewExcel")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> PreviewSupplierQuotationExcel([FromForm] IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return BadRequest("File Excel không hợp lệ");

            try
            {
                var result = await _iPRFQService.PreviewExcelProductsAsync(excelFile);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// delete PRFQ with status=4 (draft)
        /// https://localhost:7213/api/PRFQ/deletePRFQ/{prfqId}
        /// </summary>
        /// <param name="prfqId"></param>
        /// <returns></returns>
        [HttpDelete("deletePRFQ/{prfqId:int}")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> DeletePRFQ(int prfqId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var result = await _iPRFQService.DeletePRFQAsync(prfqId, userId);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// view detail PRFQ
        /// https://localhost:7213/api/PRFQ/detail/{prfqId}
        /// </summary>
        /// <param name="prfqId"></param>
        /// <returns></returns>
        [HttpGet("detail/{prfqId:int}")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> GetPRFQDetail(int prfqId)
        {
            var result = await _iPRFQService.GetPRFQDetailAsync(prfqId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Getall PRFQ
        /// https://localhost:7213/api/PRFQ/getAll
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAll")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> GetAllPRFQ()
        {
            var result = await _iPRFQService.GetAllPRFQAsync();
            return HandleServiceResult(result);
        }
    }
}
