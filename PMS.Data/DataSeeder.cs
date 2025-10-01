using Microsoft.AspNetCore.Identity;
using PMS.Data.DatabaseConfig;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(PMSContext context, RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles 
            foreach (var role in UserRoles.ALL)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
