
using PMS.Application.DTOs.Profile;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.DTOs.StockExportOrder;
using PMS.Application.DTOs.Warehouse;
using PMS.Application.DTOs.WarehouseLocation;
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
                .ForMember(dest => dest.ProductID, opt => opt.MapFrom(src => src.Product.ProductID))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Product.Unit));

            CreateMap<TaxPolicy, TaxPolicyDTO>();

            CreateMap<SalesQuotation, SalesQuotationDTO>()
                .ForMember(dest => dest.RequestCode, opt => opt.MapFrom(src => src.RequestSalesQuotation.RequestCode));

            CreateMap<SalesQuotationNote, SalesQuotationNoteDTO>();

            CreateMap<Warehouse, WarehouseDTO>();

            CreateMap<WarehouseLocation, WarehouseLocationDTO>();

            CreateMap<WarehouseLocation, WarehouseLocationDetailsDTO>()
                .IncludeBase<WarehouseLocation, WarehouseLocationDTO>()
                .ForMember(dest => dest.LotProduct, opt => opt.MapFrom(src => src.LotProducts));

            CreateMap<LotProduct, LotProductDTO>()
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier.Name))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName));

            CreateMap<Warehouse, WarehouseDetailsDTO>()
                .IncludeBase<Warehouse, WarehouseDTO>()
                .ForMember(dest => dest.WarehouseLocations, opt => opt.MapFrom(src => src.WarehouseLocations));

            CreateMap<SalesQuotationComment, SalesQuotationCommentDTO>();

            CreateMap<StockExportOrder, ListStockExportOrderDTO>()
                .ForMember(dest => dest.SalesOrderCode, opt => opt.MapFrom(src => src.SalesOrder.SalesOrderCode))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.SalesStaff.FullName));

            CreateMap<StockExportOrderDetails, DetailsStockExportOrderDTO>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.LotProduct.Product.ProductName))
                .ForMember(dest => dest.ExpiredDate, opt => opt.MapFrom(src => src.LotProduct.ExpiredDate));

            CreateMap<StockExportOrder, ViewModelDetails>()
                .IncludeBase<StockExportOrder, ListStockExportOrderDTO>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.StockExportOrderDetails));
        }
    }
}
