using PMS.Data.UnitOfWork;
using AutoMapper;

namespace PMS.API.Services.BaseService
{
    public abstract class Service
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;

        protected Service(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
    }
}
