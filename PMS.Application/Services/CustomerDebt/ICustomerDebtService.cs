using PMS.Application.DTOs.CustomerDebt;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.CustomerDebt
{
    public interface ICustomerDebtService
    {
        Task<ServiceResult<List<CustomerDebtListItemDTO>>> GetAllCustomerDebtAsync();

        /// <summary>
        /// Lấy số tiền nợ của khách hàng theo từng tháng, lọc theo năm
        /// </summary>
        Task<ServiceResult<List<CustomerDebtByMonthDTO>>> GetCustomerDebtByMonthAsync(int year);
    }
}
