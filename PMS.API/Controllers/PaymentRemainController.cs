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
        /// POST: https://localhost:7213/api/PaymentRemain/pay-remain-request/{goodsIssueNoteId}
        /// <param name="goodsIssueNoteId"></param>
        /// <returns></returns>

        //[Authorize(Roles = UserRoles.ACOUNTANT)]
        [HttpPost("pay-remain-request/{goodsIssueNoteId}")]
        public async Task<IActionResult> CreatePaymentRemainForGoodsIssueNote(int goodsIssueNoteId)
        {
            var result = await _paymentRemainService
                .CreatePaymentRemainForGoodsIssueNoteAsync(goodsIssueNoteId);

            object? data = null;
            if (result.Data != null)
            {
                var p = result.Data;
                data = new
                {
                    p.Id,
                    p.SalesOrderId,
                    p.GoodsIssueNoteId,
                    p.Amount,
                    Status = p.Status.ToString()
                };
            }

            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data
            });
        }

        /// <summary>
        /// get list payment remain
        /// Get: https://localhost/api/PaymentRemain/list-payment-remain
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
        /// Get: https://localhost/api/PaymentRemain/payment-remain-detail/{id}
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
        /// GET: https://localhost//api/PaymentRemain/ids-by-sales-order/{salesOrderId}
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


    }
}
