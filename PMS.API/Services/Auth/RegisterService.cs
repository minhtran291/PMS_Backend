using AutoMapper;
using PMS.API.Services.Base;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.Auth
{
    public class RegisterService(IUnitOfWork unitOfWork, IMapper mapper) : Service(unitOfWork, mapper), IRegisterService
    {
    }
}
