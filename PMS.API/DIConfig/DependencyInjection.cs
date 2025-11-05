using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PMS.API.BackgroundTasks;
using System.Text;

namespace PMS.API.DIConfig
{
    public static class DependencyInjection
    {
        public static void AddJwt(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]!)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero, // ko de lech thoi gian
                };
            });

            services.AddAuthorization();
        }

        public static void AddInfrastructure(this IServiceCollection services)
        {
            var wkhtmlPath = Path.Combine(AppContext.BaseDirectory,
                "Helpers", "Pdf", "wkhtmltopdf", "libwkhtmltox.dll");

            var context = new Helpers.Pdf.CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(wkhtmlPath);
        }

        public static void AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<SalesQuotationStatusUpdater>();
        }
    }
}
