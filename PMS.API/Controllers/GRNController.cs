using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.API.Services.GRNService;
using PMS.Application.DTOs.GRN;
using PMS.Core.Domain.Constant;
using PMS.Core.DTO.Content;
using PMS.Data.UnitOfWork;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GRNController : BaseController
    {
        private readonly IGRNService _IGRNService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GRNController> _logger;
        public GRNController(IGRNService IGRNService, ILogger<GRNController> logger, IUnitOfWork unitOfWork )
        {
            _IGRNService = IGRNService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// https://localhost:7213/api/GRN/createGRNFromPo/{poid}
        /// </summary>
        /// <param name="poid"></param>
        /// <returns></returns>
        [HttpPost("createGRNFromPo/{poId:int}")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
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

            return StatusCode(result.StatusCode, new
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data   
            });
        }

        /// <summary>
        /// https://localhost:7213/api/GRN/getAll
        /// Lấy toàn bộ danh sách phiếu nhập kho (Good Receipt Note)
        /// </summary>
        [HttpGet("getAll")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> GetAllGRN()
        {
            var result = await _IGRNService.GetAllGRN();
            return HandleServiceResult(result);
        }


        /// <summary>
        /// https://localhost:7213/api/GRN/detail/{id}
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("detail/{id}")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _IGRNService.GetGRNDetailAsync(id);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// Xuất phiếu nhập kho (GRN) ra file PDF
        /// https://localhost:7213/api/GRN/exportPdf/{grnId}
        /// </summary>
        /// <param name="grnId">Mã phiếu nhập kho</param>
        /// <returns>File PDF phiếu nhập kho</returns>
        [HttpGet("exportPdf/{grnId}")]
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF)]
        public async Task<IActionResult> ExportGRNToPdf(int grnId)
        {
            var pdfBytes = await _IGRNService.GeneratePDFGRNAsync(grnId);
            return File(pdfBytes, "application/pdf", $"GRN_{grnId}.pdf");
        }


        /// <summary>
        /// http://localhost:5137/api/GRN/ImportStatisticsByMonth/
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet("ImportStatisticsByMonth/{year}")]
        public async Task<IActionResult> GetImportStatsByMonth(int year)
        {
            var grns = await _unitOfWork.GoodReceiptNote.Query()
                .Where(g => g.CreateDate.Year == year)
                .Include(g => g.GoodReceiptNoteDetails)
                    .ThenInclude(d => d.Product)
                .ToListAsync();

            if (!grns.Any())
                return Ok(new ImportStatisticsByMonthDto { Year = year });

            var monthlyData = grns
                .SelectMany(g => g.GoodReceiptNoteDetails)
                .GroupBy(d => d.GoodReceiptNote.CreateDate.Month)
                .Select(monthGroup =>
                {
                    int totalQuantity = monthGroup.Sum(x => x.Quantity);

                    var productList = monthGroup
                        .GroupBy(x => new { x.ProductID, x.Product.ProductName })
                        .Select(p => new ProductImportPercentageDto
                        {
                            ProductID = p.Key.ProductID,
                            ProductName = p.Key.ProductName,
                            Quantity = p.Sum(x => x.Quantity),
                            Percentage = totalQuantity == 0
                                ? 0
                                : Math.Round((decimal)p.Sum(x => x.Quantity) * 100 / totalQuantity, 2)
                        })
                        .OrderByDescending(x => x.Percentage)
                        .ToList();

                    return new MonthlyImportWithProductsDto
                    {
                        Month = monthGroup.Key,
                        TotalQuantity = totalQuantity,
                        Products = productList
                    };
                })
                .OrderBy(x => x.Month)
                .ToList();

            var result = new ImportStatisticsByMonthDto
            {
                Year = year,
                MonthlyData = monthlyData
            };

            return Ok(result);
        }
    }
}
