using AutoMapper;
using PMS.Application.DTOs.SQ;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.QuotationService
{
    public class QuotationService(IUnitOfWork unitOfWork, IMapper mapper) : Service(unitOfWork, mapper), IQuotationService
    {
        public Task<ServiceResult<IEnumerable<QuotationDTO>>> GetAllQuotationAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<List<QuotationDTO>>> GetAllQuotationsWithActiveDateAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<QuotationDTO?>> GetQuotationByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

    }
}
