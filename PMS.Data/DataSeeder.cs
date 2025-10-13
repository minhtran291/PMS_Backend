using Microsoft.AspNetCore.Identity;
using PMS.Data.DatabaseConfig;
using PMS.Core.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using PMS.Core.Domain.Entities;

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

            if (!context.Users.Any(u => u.UserName == "admin"))
            {
                var passwordHasher = new PasswordHasher<User>();

                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "admin",
                    Email = "pmsadmin@gmail.com",
                    NormalizedEmail = "PMSADMIN@GMAIL.COM",
                    NormalizedUserName = "ADMIN",
                    UserStatus = Core.Domain.Enums.UserStatus.Active,
                    FullName = "PMS Admin",
                    Avatar = "https://as2.ftcdn.net/v2/jpg/03/31/69/91/1000_F_331699188_lRpvqxO5QRtwOM05gR50ImaaJgBx68vi.jpg",
                    Address = "Ha Noi",
                    Gender = true,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = false,
                    CreateAt = DateTime.Now
                };

                user.PasswordHash = passwordHasher.HashPassword(user, "Pmsadmin!");
                _ = await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.ADMIN);

                if (adminRole != null)
                {
                    await context.UserRoles.AddAsync(new IdentityUserRole<string>()
                    {
                        RoleId = adminRole.Id,
                        UserId = user.Id,
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
