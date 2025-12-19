
using PMS.Application.DTOs.GoodsIssueNote;
using PMS.Application.DTOs.Profile;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.DTOs.StockExportOrder;
using PMS.Application.DTOs.TaxPolicy;
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

            CreateMap<TaxPolicy, DTOs.SalesQuotation.TaxPolicyDTO>();

            CreateMap<SalesQuotation, SalesQuotationDTO>()
                .ForMember(dest => dest.RequestCode, opt => opt.MapFrom(src => src.RequestSalesQuotation.RequestCode))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.RequestSalesQuotation.CustomerProfile.User.FullName));

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

            CreateMap<SalesQuotationComment, SalesQuotationCommentDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName));

            CreateMap<StockExportOrder, ListStockExportOrderDTO>()
                .ForMember(dest => dest.SalesOrderCode, opt => opt.MapFrom(src => src.SalesOrder.SalesOrderCode))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.SalesStaff.FullName));

            CreateMap<StockExportOrderDetails, DetailsStockExportOrderDTO>()
                .ForMember(dest => dest.LotId, opt => opt.MapFrom(src => src.LotProduct.LotID))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.LotProduct.Product.ProductName))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.LotProduct.Product.Unit))
                .ForMember(dest => dest.ExpiredDate, opt => opt.MapFrom(src => src.LotProduct.ExpiredDate))
                .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.LotProduct.WarehouseLocation.LocationName))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.LotProduct.WarehouseLocation.Warehouse.Name));

            CreateMap<StockExportOrder, ViewModelDetails>()
                .IncludeBase<StockExportOrder, ListStockExportOrderDTO>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.StockExportOrderDetails));

            CreateMap<GoodsIssueNote, GoodsIssueNoteListDTO>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.WarehouseStaff.FullName))
                .ForMember(dest => dest.StockExportOrderCode, opt => opt.MapFrom(src => src.StockExportOrder.StockExportOrderCode))
                .ForMember(dest => dest.SalesOrderCode, opt => opt.MapFrom(src => src.StockExportOrder.SalesOrder.SalesOrderCode));

            CreateMap<GoodsIssueNoteDetails, GoodsIssueNoteDetailsDTO>()
                .ForMember(dest => dest.WarehouseLocationName, opt => opt.MapFrom(src => src.LotProduct.WarehouseLocation.LocationName))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.LotProduct.Product.ProductName))
                .ForMember(dest => dest.ExpiredDate, opt => opt.MapFrom(src => src.LotProduct.ExpiredDate));

            CreateMap<GoodsIssueNote, GoodsIssueNoteWithDetailsDTO>()
                .IncludeBase<GoodsIssueNote, GoodsIssueNoteListDTO>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.GoodsIssueNoteDetails));

            CreateMap<TaxPolicy, DTOs.TaxPolicy.TaxPolicyDTO>();
        }
    }
}
