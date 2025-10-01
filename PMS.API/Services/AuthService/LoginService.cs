using AutoMapper;
using PMS.API.Services.BaseService;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.AuthService
{
    public class LoginService : Service, ILoginService
    {
        public LoginService(IUnitOfWork unitOfWork, IMapper mapper) 
            : base(unitOfWork, mapper) { }
    }
}
