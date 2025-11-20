using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.CustomerDebt;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.CustomerDebt
{
    public class CustomerDebtService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CustomerDebtService> logger) : Service(unitOfWork, mapper), ICustomerDebtService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<CustomerDebtService> _logger = logger;

        public async Task<ServiceResult<List<CustomerDebtListItemDTO>>> GetAllCustomerDebtAsync()
        {
            try
            {
                var query = _unitOfWork.CustomerDebt.Query()
                    .Include(cd => cd.SalesOrder)
                        .ThenInclude(so => so.Customer)
                            .ThenInclude(cp => cp.CustomerProfile) ;

                var data = await query
                    .Select(cd => new CustomerDebtListItemDTO
                    {
                        Id = cd.Id,
                        CustomerId = cd.CustomerId,
                        SalesOrderId = cd.SalesOrderId,
                        SalesOrderCode = cd.SalesOrder.SalesOrderCode,
                        Status = cd.status,
                        DueDate = cd.SalesOrder.SalesOrderExpiredDate,
                        TotalAmount = cd.SalesOrder.TotalPrice,
                        DebtAmount = cd.DebtAmount,
                        CustomerName = cd.SalesOrder.Customer.CustomerProfile.User.FullName,
                    })
                    .OrderBy(x => x.DueDate)
                    .ToListAsync();

                return ServiceResult<List<CustomerDebtListItemDTO>>
                    .SuccessResult(data, "Lấy danh sách nợ thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllCustomerDebtAsync error");
                return ServiceResult<List<CustomerDebtListItemDTO>>
                    .Fail("Có lỗi xảy ra khi lấy danh sách nợ.", 500);
            }
        }

        public async Task<ServiceResult<List<CustomerDebtByMonthDTO>>> GetCustomerDebtByMonthAsync(int year)
        {
            try
            {
                var query = _unitOfWork.CustomerDebt.Query()
                    .Include(cd => cd.SalesOrder)
                    .Where(cd =>
                        cd.SalesOrder.SalesOrderExpiredDate.Year == year &&
                        cd.status != CustomerDebtStatus.NoDebt &&
                        cd.status != CustomerDebtStatus.Disable);

                var data = await query
                    .GroupBy(cd => cd.SalesOrder.SalesOrderExpiredDate.Month)
                    .Select(g => new CustomerDebtByMonthDTO
                    {
                        Month = g.Key,
                        TotalDebt = g.Sum(x => x.DebtAmount)
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync();

                return ServiceResult<List<CustomerDebtByMonthDTO>>
                    .SuccessResult(data, "Lấy số tiền nợ theo tháng thành công.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerDebtByMonthAsync({Year}) error", year);
                return ServiceResult<List<CustomerDebtByMonthDTO>>
                    .Fail("Có lỗi xảy ra khi lấy dữ liệu nợ theo tháng.", 500);
            }
        }
    }
}
