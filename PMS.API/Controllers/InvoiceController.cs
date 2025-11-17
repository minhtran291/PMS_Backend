using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Invoice;
using PMS.Application.Services.Invoice;
using PMS.Core.Domain.Constant;

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
        /// POST: /api/Invoice/generate-from-payment-remains
        /// Tạo hóa đơn từ danh sách PaymentRemainId (cùng 1 SalesOrder).
        /// </summary>
        /// <remarks>
        /// Body ví dụ:
        /// {
        ///   "salesOrderId": 123,
        ///   "paymentRemainIds": [ 10, 11, 12 ]
        /// }
        /// </remarks>
        [HttpPost("generate-from-payment-remains")]
        [ProducesResponseType(typeof(ServiceResult<InvoiceDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> GenerateFromPaymentRemains(
            [FromBody] GenerateInvoiceFromPaymentRemainsRequestDTO request)
        {
            var result = await _invoiceService
                .GenerateInvoiceFromPaymentRemainsAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: /api/Invoice/{id}/pdf
        /// Tạo PDF hóa đơn để in / tải về.
        /// </summary>
        [HttpGet("{id:int}/pdf")]
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
        /// POST: /api/Invoice/{id}/send-email
        /// Gửi hóa đơn cho khách hàng qua email (đính kèm PDF) và đổi trạng thái sang Send.
        /// </summary>
        [HttpPost("{id:int}/send-email")]
        public async Task<IActionResult> SendInvoiceEmail(int id)
        {
            var result = await _invoiceService.SendInvoiceEmailAsync(id);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

    }
}
