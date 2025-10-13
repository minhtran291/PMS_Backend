using PMS.Data.UnitOfWork;
using AutoMapper;
using System.Text.RegularExpressions;

namespace PMS.Application.Services.Base
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

        public static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            // Cắt trắng 2 đầu + thay thế nhiều khoảng trắng liên tục bằng 1
            var normalized = Regex.Replace(name.Trim(), @"\s+", " ");

            return normalized.ToLower(); // nếu không phân biệt hoa thường
        }
    }
}
