using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.API.Services.BaseService;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.AuthService
{
    public class TokenService : Service, ITokenService
    {
        public TokenService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper) { }

        
    }
}
