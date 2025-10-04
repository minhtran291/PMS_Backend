using PMS.Data.UnitOfWork;
using AutoMapper;
using PMS.API.Services.Auth;

namespace PMS.API.Services.Base
{
    public abstract class Service(IUnitOfWork unitOfWork, IMapper mapper)
    {
        protected readonly IUnitOfWork _unitOfWork = unitOfWork;
        protected readonly IMapper _mapper = mapper;

        public string GenerateOtp()
        {
            byte[] randomBytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            int number = BitConverter.ToInt32(randomBytes, 0) % 900000 + 100000;
            return number.ToString();
        }
    }
}
