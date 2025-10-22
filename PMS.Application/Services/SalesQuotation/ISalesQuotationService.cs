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
        Task<ServiceResult<object>> CreateSalesQuotationAsync(CreateSalesQuotationDTO dto);
        Task<ServiceResult<object>> UpdateSalesQuotationAsync(UpdateSalesQuotationDTO dto);
    }
}
