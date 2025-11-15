using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.PaymentRemain;
using PMS.Application.Services.PaymentRemainService;

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

    }
}
