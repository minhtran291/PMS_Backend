
using PMS.Application.DTOs.SQ;
using PMS.Core.Domain.Constant;

namespace PMS.API.Services.QuotationService
{
    public interface IQuotationService
    {
        Task<ServiceResult<IEnumerable<QuotationDTO>>> GetAllQuotationAsync();

        Task<ServiceResult<List<QuotationDTO>>> GetAllQuotationsWithActiveDateAsync();

        Task<ServiceResult<QuotationDTO?>> GetQuotationByIdAsync(int id);
    }
}
