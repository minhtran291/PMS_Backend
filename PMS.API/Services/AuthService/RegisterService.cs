using AutoMapper;
using PMS.API.Services.BaseService;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.AuthService
{
    public class RegisterService : Service, IRegisterService
    {
        public RegisterService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper) { }
    }
}
