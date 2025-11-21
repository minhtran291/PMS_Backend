using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.CustomerDebt;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerDebtController : ControllerBase
    {
        private readonly ICustomerDebtService _customerDebtService;

        public CustomerDebtController(ICustomerDebtService customerDebtService)
        {
            _customerDebtService = customerDebtService;
        }

        /// <summary>
        /// http://localhost:5137/api/CustomerDebt/customer-debt-list
        /// Lấy toàn bộ danh sách CustomerDebt (dùng cho màn list)
        /// </summary>
        [HttpGet("customer-debt-list")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _customerDebtService.GetAllCustomerDebtAsync();
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Lấy số tiền khách hàng nợ theo từng tháng, lọc theo năm
        /// GET: http://localhost:5137/api/CustomerDebt/by-month?year=2025
        /// </summary>
        [HttpGet("by-month")]
        public async Task<IActionResult> GetByMonth([FromQuery] int year)
        {
            var result = await _customerDebtService.GetCustomerDebtByMonthAsync(year);
            return StatusCode(result.StatusCode, new
            {
                success = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
    }
}
