using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Data.DatabaseConfig;

namespace PMS.API.Filters
{
    public static class MigrationManager
    {
        public static async Task<WebApplication> MigrateDatabase(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                using (var context = scope.ServiceProvider.GetRequiredService<PMSContext>())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                    await context.Database.MigrateAsync();

                    await DataSeeder.SeedAsync(context, roleManager);
                }
            }
            return app;
        }
    }
}
