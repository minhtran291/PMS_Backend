using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Invoice;
using PMS.Application.Services.Invoice;
using PMS.Core.Domain.Constant;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// POST: http://localhost:5137/api/Invoice/generate-from-goods-issue-note
        /// Tạo hóa đơn từ danh sách PaymentRemainId (cùng 1 SalesOrder).
        /// </summary>
        /// <remarks>
        /// Body ví dụ:
        /// {
        ///   "salesOrderCode": 123,
        ///   "GoodsIssueNoteCodes": [ 10, 11, 12 ]
        /// }
        /// </remarks>
        [HttpPost("generate-from-goods-issue-note")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GenerateFromPaymentRemains(
            [FromBody] GenerateInvoiceFromGINRequestDTO request)
        {
            var result = await _invoiceService
                .GenerateInvoiceFromGINAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137//api/Invoice/{id}/pdf
        /// Tạo PDF hóa đơn để in / tải về.
        /// </summary>
        [HttpGet("{id}/pdf")]
        [Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.MANAGER + "," + UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> GetInvoicePdf(int id)
        {
            var result = await _invoiceService.GenerateInvoicePdfAsync(id);

            if (!result.Success || result.Data == null)
            {
                return StatusCode(result.StatusCode, new
                {
                    success = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }

            var fileName = string.IsNullOrWhiteSpace(result.Data.FileName)
                ? $"invoice-{id}.pdf"
                : result.Data.FileName;

            return File(result.Data.PdfBytes,
                "application/pdf",
                fileName);
        }

        /// <summary>
        /// POST: http://localhost:5137//api/Invoice/{id}/send-email
        /// Gửi hóa đơn cho khách hàng qua email (đính kèm PDF) và đổi trạng thái sang Send.
        /// </summary>
        [HttpPost("{id}/send-email")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> SendInvoiceEmail(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }
            var result = await _invoiceService.SendInvoiceEmailAsync(id, currentUserId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137/api/Invoice/get-all/invoices
        /// Lấy toàn bộ danh sách Invoice
        /// </summary>
        [HttpGet("get-all/invoices")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _invoiceService.GetAllInvoicesAsync();
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137/api/Invoice/{id}/invoice/details
        /// Xem chi tiết 1 Invoice
        /// </summary>
        [HttpGet("{id}/invoice/details")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.CUSTOMER + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _invoiceService.GetInvoiceByIdAsync(id);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// http://localhost:5137/api/Invoice/{id}/update/draft-invoice
        /// Sửa Invoice (thêm/bớt PaymentRemain) khi Invoice còn Draft
        /// </summary>
        [HttpPut("{id}/update/draft-invoice")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> UpdateInvoiceDraft(
            int id,
            [FromBody] InvoiceUpdateDTO request)
        {
            var result = await _invoiceService.UpdateInvoiceGoodsIssueNotesAsync(id, request);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// GET: http://localhost:5137/api/Invoice/sales-order-codes
        /// Lấy danh sách tất cả SalesOrderCode (distinct, sort tăng dần).
        /// </summary>
        [HttpGet("sales-order-codes")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetAllSalesOrderCodes()
        {
            var result = await _invoiceService.GetAllSalesOrderCodesAsync();

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137/api/Invoice/{salesOrderCode}/goods-issue-note-codes
        /// Lấy toàn bộ GoodsIssueNoteCode thuộc về một SalesOrderCode.
        /// </summary>
        /// <param name="salesOrderCode">Mã SalesOrder</param>
        [HttpGet("{salesOrderCode}/goods-issue-note-codes")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetGoodsIssueNoteCodesBySalesOrderCode(string salesOrderCode)
        {
            var result = await _invoiceService
                .GetGoodsIssueNoteCodesBySalesOrderCodeAsync(salesOrderCode);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// GET:  http://localhost:5137/api/Invoice/my-invoices
        /// Lấy danh sách hóa đơn của customer đang đăng nhập.
        /// </summary>
        [HttpGet("my-invoices")]
        [Authorize(Roles = UserRoles.CUSTOMER + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetMyInvoices()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Không xác định được user hiện tại.",
                    data = (object?)null
                });
            }

            var result = await _invoiceService.GetInvoicesForCurrentCustomerAsync(userId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


        /// <summary>
        /// POST: api/Invoice/{id}/sign-smartca
        /// Tạo giao dịch ký số Invoice bằng VNPT SmartCA.
        /// Body: { "userId": "MST/CCCD", "password": "xxxx", "otp": "123456" }
        /// </summary>
        [HttpPost("{id}/sign-smartca")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> SignInvoiceWithSmartCA(
            int id,
            [FromBody] SmartCASignInvoiceRequestDTO request)
        {
            var result = await _invoiceService.CreateSmartCASignTransactionAsync(id, request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost("{id}/smartca-sign-test")]
        public async Task<IActionResult> TestSmartCASign(int id, [FromBody] SmartCASignInvoiceRequestDTO req)
        {
            var result = await _invoiceService.CreateSmartCASignTransactionAsync(id, req);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// DELETE: http://localhost:5137/api/Invoice/{id}/delete-draft
        /// Xóa Invoice khi còn ở trạng thái Draft.
        /// </summary>
        [HttpDelete("{id}/delete-draft")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> DeleteDraftInvoice(int id)
        {
            var result = await _invoiceService.DeleteDraftInvoiceAsync(id);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// POST: api/Invoice/{invoiceId}/send-late-reminder
        /// Dùng cho trường hợp remind invoice bị quá hạn thanh toán
        /// <param name="invoiceId"></param>
        /// <returns></returns>
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.MANAGER)]
        [HttpPost("{invoiceId}/send-late-reminder")]
        public async Task<IActionResult> SendLateReminder(int invoiceId)
        {
            var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var result = await _invoiceService.SendLateReminderEmailAsync(invoiceId, currentUserId!);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }


    }
}
