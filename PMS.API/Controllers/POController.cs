using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs.PO;
using PMS.Application.Services.PO;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POController : BaseController
    {
        private readonly IPOService _poService;
        private readonly IUnitOfWork _unitOfWork;
        public POController(IPOService poService, IUnitOfWork unitOfWork)
        {
            _poService = poService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// https://localhost:7213/api/PO/getAllPo
        /// </summary>
        /// <returns></returns>
        /// 
        [HttpGet("getAllPo")]
        [Authorize(Roles = $"{UserRoles.ACCOUNTANT},{UserRoles.PURCHASES_STAFF},{UserRoles.WAREHOUSE_STAFF},{UserRoles.MANAGER}")]
        public async Task<IActionResult> GetAllPurchaseOrders()
        {
            var result = await _poService.GetAllPOAsync();
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/PO/DepositedPurchaseOrder/{poid}
        /// ghi nhận tiền gửi
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("DepositedPurchaseOrder/{poid}")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = $"{UserRoles.ACCOUNTANT},{UserRoles.PURCHASES_STAFF},{UserRoles.WAREHOUSE_STAFF},{UserRoles.MANAGER}")]
        public async Task<IActionResult> GetPurchaseOrderDetail(int poid)
        {
            var result = await _poService.ViewDetailPObyID(poid);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// Lấy chi tiết đơn mua hàng (PO) theo ID kèm theo số lượng còn lại
        /// https://localhost:7213/api/PO/GetPoDetailByPoId2/poid
        /// </summary>
        [HttpGet("GetPoDetailByPoId2/{poid}")]
        [Authorize(Roles = $"{UserRoles.ACCOUNTANT},{UserRoles.PURCHASES_STAFF},{UserRoles.WAREHOUSE_STAFF},{UserRoles.MANAGER}")]
        public async Task<IActionResult> GetPurchaseOrderDetail2(int poid)
        {
            var result = await _poService.ViewDetailPObyID2(poid);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// https://localhost:7213/api/PO/{poid}/status?newStatus=approved
        /// </summary>
        /// <param name="poid"></param>
        /// <param name="newStatus"></param>
        /// <returns></returns>
        [HttpPut("{poid}/status")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
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
        [Authorize(Roles = UserRoles.WAREHOUSE_STAFF + "," + UserRoles.MANAGER)]
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



        /// <summary>
        /// https://localhost:7213/api/PO/GetPharmacySecretInfor
        /// lay thong tin kinh doanh 
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetPharmacySecretInfor")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetPharmacySecretInfor()
        {
            var result= await _poService.PharmacySecretInfor();
            return HandleServiceResult(result);
        }



        /// <summary>
        /// https://localhost:7213/api/PO/GetAllDebtReport
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAllDebtReport")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetAllDebtReport()
        {
            var result = await _poService.GetAllDebtReport();
            return HandleServiceResult(result);
        }


        /// <summary>
        /// https://localhost:7213/api/PO/GetDetailDebtReport/{dbid}
        /// </summary>
        /// <param name="dbid"></param>
        /// <returns></returns>
        [HttpGet("GetDetailDebtReport/{dbid}")]
        //[Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> GetDetailDebtReport(int dbid)
        {
            var result = await _poService.GetDebtReportDetail(dbid);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// http://localhost:5137/api/PO/detailsByYear/?year=2025
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet("detailsByYear")]
        public async Task<IActionResult> GetPurchasingOrderDetailsByYear(int year)
        {
            var poList = await _unitOfWork.PurchasingOrder.Query()
                .Where(p => p.OrderDate.Year == year
                         && (p.Status == PurchasingOrderStatus.paid
                          || p.Status == PurchasingOrderStatus.deposited || p.Status == PurchasingOrderStatus.compeleted))
                .Include(p => p.PurchasingOrderDetails)
                .Include(p => p.User)
                .Include(p=>p.Quotations)
                .ToListAsync();          
            var grouped = poList
                .GroupBy(p => p.OrderDate.Month)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    
                    Month = g.Key,
                    TotalOrders = g.Count(),
                    Orders = g.Select(p => new PurchasingOrderDetailByMonthDto
                    {
                        POID = p.POID,
                        OrderDate = p.OrderDate,
                        Total = p.Total,
                        Deposit = p.Deposit,
                        Debt = p.Debt,
                        Status = p.Status,
                        supname= p.Quotations.SupplierID, 
                        QID = p.QID,
                        CreatedBy = p.User?.UserName ?? "",
                        Details = p.PurchasingOrderDetails.Select(d => new PurchasingOrderDetailItemDto
                        {
                            PODID = d.PODID,
                            ProductID = d.ProductID,
                            ProductName = d.ProductName,
                            DVT = d.DVT,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            UnitPriceTotal = d.UnitPriceTotal,
                            Tax = d.Tax,
                            ExpiredDate = d.ExpiredDate
                        }).ToList()
                    }).ToList()
                })
                .ToList();

            return Ok(new
            {
                Year = year,
                TotalMonths = grouped.Count,
                Data = grouped
            });
        }

        /// <summary>
        /// http://localhost:5137/api/PO/UploadPdf
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost("UploadPdf")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            
            const long maxSize = 5 * 1024 * 1024;
            if (file.Length > maxSize)
                return BadRequest("File size cannot exceed 5MB.");

           
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf")
                return BadRequest("Only PDF files are allowed.");


            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);


            string fileName = $"{Guid.NewGuid()}.pdf";
            string fullPath = Path.Combine(folderPath, fileName);


            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            return Ok(new
            {
                message = "Upload success",
                fileName = fileName,
                fileUrl = $"/pdfs/{fileName}"
            });
        }

        /// <summary>
        /// http://localhost:5137/api/PO/DownloadPdf/?fileName=
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpGet("DownloadPdf")]
        public IActionResult DownloadPdf([FromQuery] string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name is required");

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
            var fullPath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found");

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, "application/pdf", fileName);
        }


        /// <summary>
        /// http://localhost:5137/api/PO/purchasingOrderProducts
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet("purchasingOrderProducts")]
        public async Task<IActionResult> GetApprovedOrderProducts()
        {

            var result = await _poService.GetPendingReceivingProductsAsync();
            return HandleServiceResult(result); 
        }

    }
}
