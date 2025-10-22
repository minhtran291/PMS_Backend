
using PMS.Application.DTOs.Profile;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Identity;

namespace PMS.Application.Automapper
{
    public class ApplicationMapper : AutoMapper.Profile
    {
        public ApplicationMapper()
        {
            CreateMap<User, CommonProfileDTO>();

            CreateMap<User, StaffProfileDTO>()
                .IncludeBase<User, CommonProfileDTO>()
                .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => src.StaffProfile != null ? src.StaffProfile.EmployeeCode : "Không có dữ liệu"));

            CreateMap<User, CustomerProfileDTO>()
                .IncludeBase<User, CommonProfileDTO>()
                .ForMember(dest => dest.MST, otp => otp.MapFrom(src => src.CustomerProfile != null ? src.CustomerProfile.Mst : null))
                .ForMember(dest => dest.ImageCnkd, otp => otp.MapFrom(src => src.CustomerProfile != null ? src.CustomerProfile.ImageCnkd : null))
                .ForMember(dest => dest.ImageByt, otp => otp.MapFrom(src => src.CustomerProfile != null ? src.CustomerProfile.ImageByt : null))
                .ForMember(dest => dest.Mshkd, otp => otp.MapFrom(src => src.CustomerProfile != null ? src.CustomerProfile.Mshkd : null));

            CreateMap<LotProduct, LotDTO>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Product.Unit));

            CreateMap<TaxPolicy, TaxPolicyDTO>();

            CreateMap<SalesQuotationValidity, SalesQuotationValidityDTO>();
        }
    }
}
