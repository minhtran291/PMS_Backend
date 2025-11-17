using PMS.Application.DTOs.TaxPolicy;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.TaxPolicy
{
    public interface ITaxPolicySerivce
    {
        Task<ServiceResult<object>> CreateAsync(CreateTaxPolicyDTO dto);
        Task<ServiceResult<object>> UpdateAsync(UpdateTaxPolicyDTO dto);
        Task<ServiceResult<object>> DisableEnableAsync(int taxId);
        Task<ServiceResult<object>> DeleteAsync(int taxId);
        Task<ServiceResult<object>> ListAsync();
        Task<ServiceResult<object>> DetailsAsync(int taxId);
    }
}
