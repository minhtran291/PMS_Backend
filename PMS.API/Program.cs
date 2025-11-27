using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using PMS.API.DIConfig;
using PMS.API.Helpers.PermisstionStaff;
using PMS.Application.DIConfig;
using PMS.Application.Filters;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Identity;
using PMS.Data.DatabaseConfig;

namespace PMS.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ExcelPackage.License.SetNonCommercialPersonal("hoanganh");
            // Add services to the container.
            // DbContext
            // ======== DATABASE ========
            builder.Services.AddDatabaseContext(builder.Configuration);

            // ======== NOTIFICATION ========
            builder.Services.AddScoped<INotificationSender, SignalRNotificationSender>();

            // ======== IDENTITY ========
            builder.Services.AddIdentityConfig(); 

            // ======== AUTHORIZATION POLICY ========
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Permissions.CAN_APPROVE_CUSTOMER, policy =>
                    policy.RequireClaim("Permission", Permissions.CAN_APPROVE_CUSTOMER));
            });
            //signalR
            // ======== SIGNALR ========
            builder.Services.AddSignalR();

            // ======== SERVICES, REPOSITORIES, INFRA ========
            builder.Services.AddApplicationAutoMapper();
            builder.Services.AddRepositories();
            builder.Services.AddServices();
            builder.Services.AddInfrastructure();
            builder.Services.AddExternalServices();
            builder.Services.InitialValueConfig(builder.Configuration);

            // ======== CONTROLLERS ========
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            // ======== JWT CONFIG ========
            var jwtKey = builder.Configuration["Jwt:SecretKey"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new Exception("Jwt:Key is not configured");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<User>>(); // Nếu bạn dùng tên khác, thay vào

                        var user = await userManager.GetUserAsync(context.Principal);
                        if (user == null) return;

                        var identity = context.Principal.Identity as ClaimsIdentity;
                        var claims = await userManager.GetClaimsAsync(user);
                        identity?.AddClaims(claims);
                    }
                };
            });

            // ======== BACKGROUND SERVICES ========
            builder.Services.AddBackgroundServices();

            // ======== CORS ========
            // cho phep khi dung ajax goi api tu fe den be
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // ======== REDIS ========
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "PO_Excel_";
            });

            // ======== SWAGGER ========
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyAPI", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            });

            builder.Services.AddDataProtection();

            var app = builder.Build();

            await app.MigrateDatabase();

            app.UseStaticFiles();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                });
            }

            app.UseCors("AllowFrontend");

            app.UseHttpsRedirection();

            // ======== AUTH ========
            app.UseAuthentication();
            app.UseAuthorization();

            // ======== SIGNALR HUB ========
            app.MapHub<NotificationHub>("/notificationHub");
            app.MapHubs();

            app.MapControllers();

            app.Run();
        }
    }
}
