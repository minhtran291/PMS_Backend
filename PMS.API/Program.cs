
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using PMS.API.DIConfig;
using PMS.Application.DIConfig;
using PMS.Application.Filters;
using PMS.Application.Services.Notification;
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
            builder.Services.AddDatabaseContext(builder.Configuration);
            builder.Services.AddScoped<INotificationSender, SignalRNotificationSender>();

            //signalR
            builder.Services.AddSignalR();

            // Identity
            builder.Services.AddIdentityConfig();

            // Add Config Options
            builder.Services.InitialValueConfig(builder.Configuration);

            // Add Auto mapper
            builder.Services.AddApplicationAutoMapper();

            // Repository
            builder.Services.AddRepositories();

            // Services
            builder.Services.AddServices();

            // External Services
            builder.Services.AddExternalServices();

            // Controller + JSON settings
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            // JWT
            builder.Services.AddJwt(builder.Configuration);

            // cho phep khi dung ajax goi api tu fe den be
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:4200")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                });
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
                                Id = "Bearer",
                            }
                        },
                        new string[]{}
                    }
                });
            });
            // Ma hoa so tai khoan ngan hang
            builder.Services.AddDataProtection();
            var app = builder.Build();

            await app.MigrateDatabase();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowFrontend");

            app.UseHttpsRedirection();

            // Enable static files serving
            app.UseStaticFiles();

            app.UseAuthentication(); // tu them vao
            // kt thong tin dang nhap cua ng dung => ng nay la ai?
            // theo thu tu Authentication trc roi moi den Authorization
            // co thong tin roi moi phan quuyen dc

            app.UseAuthorization();
            // kt quyen han => ng nay co dc phep lam viec nay ko?

            //maphub
            app.MapHub<NotificationHub>("/notificationHub");

            app.MapControllers();

            app.Run();
        }
    }
}
