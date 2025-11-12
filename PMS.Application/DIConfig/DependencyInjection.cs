using AutoMapper;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PMS.API.Services.GRNService;
using PMS.API.Services.POService;
using PMS.API.Services.PRFQService;
using PMS.API.Services.QuotationService;
using PMS.Application.Automapper;
using PMS.Application.DTOs.VNPay;
using PMS.Application.Services.Admin;
using PMS.Application.Services.Auth;
using PMS.Application.Services.Category;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Application.Services.PO;
using PMS.Application.Services.Product;
using PMS.Application.Services.RequestSalesQuotation;
using PMS.Application.Services.SalesQuotation;
using PMS.Application.Services.SalesOrder;
using PMS.Application.Services.Supplier;
using PMS.Application.Services.User;
using PMS.Application.Services.VNpay;
using PMS.Application.Services.Warehouse;
using PMS.Application.Services.WarehouseLocation;
using PMS.Core.ConfigOptions;
using PMS.Application.Services.StockExportOrder;
using PMS.Application.Services.GoodsIssueNote;
using System.ComponentModel.Design;
using PMS.API.Helpers.VnPay;
using PMS.Application.Services.VietQR;

namespace PMS.Application.DIConfig
{
    public static class DependencyInjection
    {
        public static void AddApplicationAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(ApplicationMapper));
        }

        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IUserRoleResolverService, UserRoleResolverService>();
            services.AddScoped<IWarehouseLocationService, WarehouseLocationService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IRequestSalesQuotationService, RequestSalesQuotationService>();
            services.AddScoped<IPRFQService, PRFQService>();
            services.AddScoped<IPOService, POService>();
            services.AddScoped<IGRNService, GRNService>();
            services.AddScoped<ISalesQuotationService, SalesQuotationService>();
            services.AddScoped<IQuotationService, QuotationService>();
            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<IVnPayGateway, VnPayGateway>();
            services.AddScoped<IVnPayService, VnPayService>();
            services.AddScoped<IVietQrService, VietQrService>();
            services.AddScoped<IStockExportOderService, StockExportOrderService>();
            services.AddScoped<IGoodsIssueNoteService, GoodsIssueNoteService>();
        }

        public static void InitialValueConfig(this IServiceCollection services, IConfiguration configuration)
        {
            var emailConfig = configuration.GetSection("Email");
            var jwtConfig = configuration.GetSection("Jwt");
            var VnPayConfig = configuration.GetSection("VnPay");
            var VietQRConfig = configuration.GetSection("VietQR");
            services.Configure<EmailConfig>(emailConfig);
            services.Configure<JwtConfig>(jwtConfig);
            services.Configure<VNPayConfig>(VnPayConfig);
            services.Configure<VietQRConfig>(VietQRConfig);
        }

        public static void AddExternalServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddSingleton<IConverter, SynchronizedConverter>(_ => new SynchronizedConverter(new PdfTools()));
        }
    }
}
