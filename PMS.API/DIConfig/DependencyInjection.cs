using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PMS.API.Automapper;
using PMS.API.Services.Auth;
using PMS.API.Services.ExternalService;
using PMS.API.Services.User;
using PMS.Core.ConfigOptions;
using System.Text;

namespace PMS.API.DIConfig
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
        }

        public static void InitialValueConfig(this IServiceCollection services, IConfiguration configuration)
        {
            var emailConfig = configuration.GetSection("Email");
            var jwtConfig = configuration.GetSection("Jwt");
            services.Configure<EmailConfig>(emailConfig);
            services.Configure<JwtConfig>(jwtConfig);
        }

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

        public static void AddExternalServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
        }
    }
}
