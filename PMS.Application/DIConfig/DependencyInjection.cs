using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PMS.Application.Automapper;
using PMS.Application.Services.Admin;
using PMS.Application.Services.Auth;
using PMS.Application.Services.Category;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Product;
using PMS.Application.Services.Supplier;
using PMS.Application.Services.User;
using PMS.Application.Services.Warehouse;
using PMS.Application.Services.WarehouseLocation;
using PMS.Core.ConfigOptions;
using PMS.Data.Repositories.Notification;

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
            //services.AddScoped<PMS.Application.Services.Notification.INotificationSender>();
            services.AddScoped<PMS.Application.Services.Notification.IUserRoleResolverService,
                PMS.Application.Services.Notification.UserRoleResolverService> ();
            services.AddScoped<IWarehouseLocationService, WarehouseLocationService>();
            services.AddScoped<PMS.Application.Services.Notification.INotificationService,
                PMS.Application.Services.Notification.NotificationService > ();
}

        public static void InitialValueConfig(this IServiceCollection services, IConfiguration configuration)
        {
            var emailConfig = configuration.GetSection("Email");
            var jwtConfig = configuration.GetSection("Jwt");
            services.Configure<EmailConfig>(emailConfig);
            services.Configure<JwtConfig>(jwtConfig);
        }

        public static void AddExternalServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
        }
    }
}
