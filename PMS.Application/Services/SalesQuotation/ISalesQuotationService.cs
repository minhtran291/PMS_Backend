using PMS.Application.DTOs.SalesQuotation;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.SalesQuotation
{
    public interface ISalesQuotationService
    {
        Task<ServiceResult<object>> GenerateFormAsync(int rsqId);
        Task<ServiceResult<object>> CreateSalesQuotationAsync(CreateSalesQuotationDTO dto, string ssId);
        Task<ServiceResult<object>> UpdateSalesQuotationAsync(UpdateSalesQuotationDTO dto, string ssId);
        Task<ServiceResult<object>> DeleteSalesQuotationAsync(int sqId, string ssId);
        Task<ServiceResult<List<SalesQuotationDTO>>> SalesQuotationListAsync(string role, string ssId);
    }
}
