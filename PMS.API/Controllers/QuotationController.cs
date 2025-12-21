using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Services.QuotationService;
using PMS.Core.Domain.Constant;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuotationController : BaseController
    {
        private readonly IQuotationService _quotationService;
        public QuotationController(IQuotationService quotationService)
        {
            _quotationService = quotationService;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách báo giá
        /// https://localhost:7213/api/Quotation/getAllSupplierResponseQuotation
        /// </summary>
        /// <returns>Danh sách QuotationDTO</returns>
        [HttpGet("getAllSupplierResponseQuotation")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetAllQuotationAsync()
        {
            var result = await _quotationService.GetAllQuotationAsync();
            return HandleServiceResult(result);
        }


        /// <summary>
        /// https://localhost:7213/api/Quotation/getAllWithStatus
        /// Lấy tất cả báo giá kèm trạng thái (InDate / OutOfDate) realtime
        /// </summary>
        [HttpGet("getAllWithStatus")]
        [Authorize(Roles = UserRoles.PURCHASES_STAFF + "," + UserRoles.MANAGER)]
        public async Task<IActionResult> GetAllQuotationsWithActiveDateAsync()
        {
            var result = await _quotationService.GetAllQuotationsWithActiveDateAsync();
            return HandleServiceResult(result);
        }

        /// <summary>
        /// https://localhost:7213/api/Quotation/detailSupplierResponseQuotation/{id}
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("detailSupplierResponseQuotation/{id}")]
        //[Authorize(Roles = UserRoles.PURCHASES_STAFF)]
        public async Task<IActionResult> GetQuotationById(int id)
        {
            var result = await _quotationService.GetQuotationByIdAsync(id);
            return HandleServiceResult(result);
        }
    }
}
