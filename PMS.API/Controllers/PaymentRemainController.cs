using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.PaymentRemain;
using PMS.Application.Services.PaymentRemainService;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentRemainController : ControllerBase
    {
        private readonly IPaymentRemainService _paymentRemainService;

        public PaymentRemainController(IPaymentRemainService paymentRemainService)
        {
            _paymentRemainService = paymentRemainService;
        }

        public class InitInvoiceVnPayRequest
        {
            public decimal? Amount { get; set; }   // null = thanh toán hết phần còn lại
            public string? Locale { get; set; } = "vn";
        }

        /// <summary>
        /// Tạo yêu cầu thanh toán phần còn lại đến Customer
        /// POST: http://localhost:5137/api/PaymentRemain/pay-remain-request
        /// /// <param name="request"></param>
        /// <returns></returns>


        [HttpPost("pay-remain-request")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> CreatePaymentRemain([FromBody] CreatePaymentRemainRequestDTO request)
        {
            var result = await _paymentRemainService.CreatePaymentRemainForInvoiceAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// get list payment remain
        /// Get: http://localhost:5137/api/PaymentRemain/list-payment-remain
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("list-payment-remain")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.CUSTOMER)]
        public async Task<IActionResult> GetList([FromQuery] PaymentRemainListRequestDTO request)
        {
            var result = await _paymentRemainService.GetPaymentRemainsAsync(request);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// get details payment remain by id
        /// Get: http://localhost:5137/api/PaymentRemain/payment-remain-detail/{id}
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("payment-remain-detail/{id}")]
        [Authorize(Roles = UserRoles.ACCOUNTANT + "," + UserRoles.CUSTOMER)]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _paymentRemainService.GetPaymentRemainDetailAsync(id);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// GET: http://localhost:5137/api/PaymentRemain/ids-by-sales-order/{salesOrderId}
        /// Lấy danh sách PaymentRemainId (Success, Remain/Full) theo SalesOrderId.
        /// </summary>
        [HttpGet("ids-by-sales-order/{salesOrderId}")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> GetIdsBySalesOrder(int salesOrderId)
        {
            var result = await _paymentRemainService
                .GetPaymentRemainIdsBySalesOrderIdAsync(salesOrderId);

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data 
            });
        }


        /// <summary>
        /// POST:  http://localhost:5137/api/PaymentRemain/{id}/success
        /// </summary>
        /// <param name="id"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost("{id}/success")]
        [Authorize(Roles = UserRoles.ACCOUNTANT)]
        public async Task<IActionResult> MarkSuccess(int id, [FromBody] MarkPaymentSuccessRequestDTO body)
        {
            var result = await _paymentRemainService
                .MarkPaymentSuccessAsync(id, body?.GatewayTransactionRef);

            return StatusCode(result.StatusCode, result);
        }


        [HttpPost("invoices/{invoiceId}/vnpay/init")]
        //[Authorize] 
        public async Task<IActionResult> InitVnPayForInvoice(
        int invoiceId,
        [FromBody] InitInvoiceVnPayRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var locale = string.IsNullOrWhiteSpace(request.Locale) ? "vn" : request.Locale;

            var result = await _paymentRemainService.InitVnPayForInvoiceAsync(
                invoiceId,
                request.Amount,
                clientIp,
                locale);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Customer tạo yêu cầu xác nhận chuyển khoản cho Invoice
        /// POST: http://localhost:5137/api/PaymentRemain/invoices/{invoiceId}/bank-transfer/check-request
        /// <param name="invoiceId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("invoices/{invoiceId}/bank-transfer/check-request")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateBankTransferCheckRequest(
            int invoiceId,
            [FromBody] CreateBankTransferCheckRequestDTO request)
        {
            var result = await _paymentRemainService
                .CreateBankTransferCheckRequestForInvoiceAsync(invoiceId, request);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Accountant approve yêu cầu
        /// POST: http://localhost:5137/api/PaymentRemain/bank-transfer/{paymentRemainId}/approve-check-request
        /// <param name="paymentRemainId"></param>
        /// <returns></returns>
        [HttpPost("bank-transfer/{paymentRemainId}/approve-check-request")]
        // [Authorize(Roles = "Accountant")]
        public async Task<IActionResult> ApproveBankTransfer(int paymentRemainId)
        {
            var result = await _paymentRemainService
                .ApproveBankTransferRequestAsync(paymentRemainId);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Accountant reject yêu cầu kèm lý do
        /// POST:http://localhost:5137/api/PaymentRemain/bank-transfer/{paymentRemainId}/reject
        /// <param name="paymentRemainId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("bank-transfer/{paymentRemainId}/reject")]
        // [Authorize(Roles = "Accountant")]
        public async Task<IActionResult> RejectBankTransfer(
            int paymentRemainId,
            [FromBody] RejectBankTransferRequestDTO request)
        {
            var result = await _paymentRemainService
                .RejectBankTransferRequestAsync(paymentRemainId, request.Reason);

            return StatusCode(result.StatusCode, result);
        }
    }
}
