using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.RequestSalesQuotation
{
    public interface IRequestSalesQuotationService
    {
        Task<ServiceResult<bool>> CreateRequestSalesQuotation(CreateRsqDTO dto, string? userId);
        Task<ServiceResult<List<ViewRsqDTO>>> ViewRequestSalesQuotationList(string? userId);
        Task<ServiceResult<ViewRsqDTO>> ViewRequestSalesQuotationDetails(int rsqId);
    }
}
