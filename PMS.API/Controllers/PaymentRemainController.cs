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

        /// <summary>
        /// Tạo yêu cầu thanh toán phần còn lại đến Customer
        /// POST: http://localhost:5137/api/PaymentRemain/pay-remain-request
        /// /// <param name="request"></param>
        /// <returns></returns>

        //[Authorize(Roles = UserRoles.ACOUNTANT)]
        [HttpPost("pay-remain-request")]
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
        [HttpGet("ids-by-sales-order/{salesOrderId:int}")]
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
        public async Task<IActionResult> MarkSuccess(int id, [FromBody] MarkPaymentSuccessRequestDTO body)
        {
            var result = await _paymentRemainService
                .MarkPaymentSuccessAsync(id, body?.GatewayTransactionRef);

            return StatusCode(result.StatusCode, result);
        }

    }
}
