using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.PRFQService;
using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.PRFQ;
using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;

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
        public async Task<IActionResult> ConvertToPurchaseOrder([FromBody] PurchaseOrderInputDto input )
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

                var result = await _iPRFQService.ConvertExcelToPurchaseOrderAsync(userId, input, input.status);
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
        /// https://localhost:7213/api/PRFQ/previewSupplierQuotaionExcel
        /// Xem trước danh sách sản phẩm trong file báo giá Excel của supplier.
        /// </summary>
        /// <param name="excelFile">File Excel do supplier gửi</param>
        /// <returns>Danh sách sản phẩm gồm ProductID, Mô tả, ĐVT, Giá báo...</returns>
        [HttpPost("previewSupplierQuotaionExcel")]
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

        //ok
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

        //ok
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

        //ok
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

        //ok
        /// <summary>
        /// Xem trước file Excel PRFQ 
        /// https://localhost:7213/api/PRFQ/preview/{prfqId}
        /// </summary>
        /// <param name="prfqId">ID của PRFQ</param>
        [HttpGet("preview/{prfqId}")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> PreviewExcel(int prfqId)
        {
            var result = await _iPRFQService.PreviewPRFQAsync(prfqId);
            return StatusCode(result.StatusCode, result);
        }

        //ok
        /// <summary>
        /// Tải xuống file Excel PRFQ 
        /// https://localhost:7213/api/PRFQ/download/{prfqId}
        /// </summary>
        /// <param name="prfqId">ID của PRFQ</param>
        [HttpGet("download/{prfqId}")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> DownloadExcel(int prfqId)
        {
            var result = await _iPRFQService.GenerateExcelAsync(prfqId);
            if (result == null)
                return NotFound("Không tìm thấy PRFQ.");

            
            Response.Headers.Append("Content-Disposition", $"attachment; filename=PRFQ_{prfqId}.xlsx");
            return File(result,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        /// <summary>
        ///  https://localhost:7213/api/PRFQ/{prfqId}/status
        /// </summary>
        /// <param name="prfqId"></param>
        /// <param name="newStatus"></param>
        /// <returns></returns>
        [HttpPut("{prfqId}/status")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> UpdatePRFQStatus(int prfqId, [FromBody] PRFQStatus newStatus)
        {
            try
            {
                await _iPRFQService.UpdatePRFQStatusAsync(prfqId, newStatus);
                return Ok(new { Message = $"Đã cập nhật trạng thái PRFQ {prfqId} thành {newStatus}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        ///  https://localhost:7213/api/PRFQ/{prfqId}/continue
        /// Tiếp tục chỉnh sửa PRFQ đang ở trạng thái Draft
        /// </summary>
        [HttpPut("{prfqId}/continue")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> ContinueEditPRFQ([FromRoute] int prfqId, [FromBody] ContinuePRFQDTO input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _iPRFQService.ContinueEditPRFQ(prfqId, input);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {             
                return StatusCode(500, new { Message = "Đã xảy ra lỗi trong quá trình chỉnh sửa PRFQ." });
            }
        }


        /// <summary>
        /// Tạo Purchase Order từ Quotation đã có sẵn trong hệ thống.
        /// https://localhost:7213/api/PRFQ/create-from-quotation
        /// </summary>
        /// <param name="input">Dữ liệu đầu vào: QID, danh sách sản phẩm và số lượng</param>
        /// <returns>Trả về POID và thông tin trạng thái</returns>
        [HttpPost("create-from-quotation")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> CreatePurchaseOrderByQuotation([FromBody] PurchaseOrderByQuotaionInputDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var result = await _iPRFQService.CreatePurchaseOrderByQIDAsync(userId, input);


            return HandleServiceResult(result);
        }

        /// <summary>
        /// Tiếp tục chỉnh sửa PO draft
        /// https://localhost:7213/api/PRFQ/continue-edit/{poid}
        /// </summary>
        /// <param name="poid">ID của PO cần chỉnh sửa</param>
        /// <param name="input">Dữ liệu input mới</param>
        [HttpPut("continue-edit/{poid}")]
        public async Task<IActionResult> ContinueEditPO([FromRoute] int poid, [FromBody] PurchaseOrderByQuotaionInputDto input)

        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var serviceResult = await _iPRFQService.CountinueEditPurchasingOrderAsync(poid, userId, input);

            
            return HandleServiceResult(serviceResult);
        }


        /// <summary>
        /// https://localhost:7213/api/PRFQ/preview2/{QID}
        /// Lấy xem trước sản phẩm theo báo giá đã chọn
        /// </summary>
        [HttpGet("preview2/{QID}")]
        public async Task<IActionResult> PreviewProductsFromQuotation(int QID)
        {
            var result = await _iPRFQService.PreviewExcelProductsByExcitedQuotationAsync(QID);
            return HandleServiceResult(result);
        }

    }
}
