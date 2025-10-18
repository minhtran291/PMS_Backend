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
        Task<ServiceResult<object>> CreateRequestSalesQuotation(CreateRsqDTO dto, string? customerProfileId);
        Task<ServiceResult<List<ViewRsqDTO>>> ViewRequestSalesQuotationList(string? customerProfileId, string? staffProfileId);
        Task<ServiceResult<object>> ViewRequestSalesQuotationDetails(int rsqId, string? customerProfileId, string? staffProfileId);
        Task<ServiceResult<object>> UpdateRequestSalesQuotation(UpdateRsqDTO dto, string? customerProfileId);
        Task<ServiceResult<object>> SendSalesQuotationRequest(string? customerProfileId, int rsqId);
        Task<ServiceResult<object>> RemoveRequestSalesQuotation(int rsqId, string? customerProfileId);
    }
}
