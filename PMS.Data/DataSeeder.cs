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

            // admin
            if (!context.Users.Any())
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
                    PhoneNumber = "0912345987",
                    Avatar = "/images/AvatarDefault.png",
                    Address = "Ha Noi",
                    Gender = true,
                    EmailConfirmed = true,
                    PhoneNumber = "0123456789",
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

                // manager
                var manager = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "manager",
                    Email = "pmsmanager@gmail.com",
                    NormalizedEmail = "PMSMANAGER@GMAIL.COM",
                    NormalizedUserName = "MANAGER",
                    UserStatus = Core.Domain.Enums.UserStatus.Active,
                    FullName = "PMS MANAGER",
                    PhoneNumber = "0912312309",
                    Avatar = "/images/AvatarDefault.png",
                    Address = "Ha Noi",
                    Gender = true,
                    EmailConfirmed = true,
                    PhoneNumber = "0123456788",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = false,
                    CreateAt = DateTime.Now
                };

                manager.PasswordHash = passwordHasher.HashPassword(manager, "Pmsmanager!");
                _ = await context.Users.AddAsync(manager);
                await context.SaveChangesAsync();

                var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.MANAGER);

                if (managerRole != null)
                {
                    await context.UserRoles.AddAsync(new IdentityUserRole<string>()
                    {
                        RoleId = managerRole.Id,
                        UserId = manager.Id,
                    });
                }
                await context.SaveChangesAsync();

                // sales staff
                var salesStaff = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "sales",
                    Email = "pmssales@gmail.com",
                    NormalizedEmail = "PMSSALES@GMAIL.COM",
                    NormalizedUserName = "SALES",
                    UserStatus = Core.Domain.Enums.UserStatus.Active,
                    FullName = "PMS SALES",
                    PhoneNumber = "0912345912",
                    Avatar = "/images/AvatarDefault.png",
                    Address = "Ha Noi",
                    Gender = true,
                    EmailConfirmed = true,
                    PhoneNumber = "0123456787",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = false,
                    CreateAt = DateTime.Now
                };

                salesStaff.PasswordHash = passwordHasher.HashPassword(salesStaff, "Pmssales!");
                _ = await context.Users.AddAsync(salesStaff);
                await context.SaveChangesAsync();

                var salesRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.SALES_STAFF);

                if (salesRole != null)
                {
                    await context.UserRoles.AddAsync(new IdentityUserRole<string>()
                    {
                        RoleId = salesRole.Id,
                        UserId = salesStaff.Id,
                    });
                }
                await context.SaveChangesAsync();

                // purchases staff
                var purchasesStaff = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "purchases",
                    Email = "pmspurchases@gmail.com",
                    NormalizedEmail = "PMSPURCHASES@GMAIL.COM",
                    NormalizedUserName = "PURCHASES",
                    UserStatus = Core.Domain.Enums.UserStatus.Active,
                    FullName = "PMS PURCHASES",
                    PhoneNumber = "0912345923",
                    Avatar = "/images/AvatarDefault.png",
                    Address = "Ha Noi",
                    Gender = true,
                    EmailConfirmed = true,
                    PhoneNumber = "0123456786",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = false,
                    CreateAt = DateTime.Now
                };

                purchasesStaff.PasswordHash = passwordHasher.HashPassword(purchasesStaff, "Pmspurchases!");
                _ = await context.Users.AddAsync(purchasesStaff);
                await context.SaveChangesAsync();

                var purchasesRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.PURCHASES_STAFF);

                if (purchasesRole != null)
                {
                    await context.UserRoles.AddAsync(new IdentityUserRole<string>()
                    {
                        RoleId = purchasesRole.Id,
                        UserId = purchasesStaff.Id,
                    });
                }
                await context.SaveChangesAsync();

                // warehouse staff
                var warehouseStaff = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "warehouse",
                    Email = "pmswarehouse@gmail.com",
                    NormalizedEmail = "PMSWAREHOUSE@GMAIL.COM",
                    NormalizedUserName = "WAREHOUSE",
                    UserStatus = Core.Domain.Enums.UserStatus.Active,
                    FullName = "PMS WAREHOUSE",
                    PhoneNumber = "0912345934",
                    Avatar = "/images/AvatarDefault.png",
                    Address = "Ha Noi",
                    Gender = true,
                    EmailConfirmed = true,
                    PhoneNumber = "0123456785",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = false,
                    CreateAt = DateTime.Now
                };

                warehouseStaff.PasswordHash = passwordHasher.HashPassword(warehouseStaff, "Pmswarehouse!");
                _ = await context.Users.AddAsync(warehouseStaff);
                await context.SaveChangesAsync();

                var warehouseRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.WAREHOUSE_STAFF);

                if (warehouseRole != null)
                {
                    await context.UserRoles.AddAsync(new IdentityUserRole<string>()
                    {
                        RoleId = warehouseRole.Id,
                        UserId = warehouseStaff.Id,
                    });
                }
                await context.SaveChangesAsync();

                // accountant staff
                var accountant = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "accountant",
                    Email = "pmsaccountant@gmail.com",
                    NormalizedEmail = "PMSACCOUNTANT@GMAIL.COM",
                    NormalizedUserName = "ACCOUNTANT",
                    UserStatus = Core.Domain.Enums.UserStatus.Active,
                    FullName = "PMS ACCOUNTANT",
                    PhoneNumber = "0912345945",
                    Avatar = "/images/AvatarDefault.png",
                    Address = "Ha Noi",
                    Gender = true,
                    EmailConfirmed = true,
                    PhoneNumber = "0123456784",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = false,
                    CreateAt = DateTime.Now
                };

                accountant.PasswordHash = passwordHasher.HashPassword(accountant, "Pmsaccountant!");
                _ = await context.Users.AddAsync(accountant);
                await context.SaveChangesAsync();

                var accountantRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.ACCOUNTANT);

                if (accountantRole != null)
                {
                    await context.UserRoles.AddAsync(new IdentityUserRole<string>()
                    {
                        RoleId = accountantRole.Id,
                        UserId = accountant.Id,
                    });
                }
                await context.SaveChangesAsync();
            }

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category
                    {
                        Name = "Thuốc giảm đau",
                        Description = "Các loại thuốc giúp giảm đau như paracetamol, ibuprofen, aspirin, v.v."
                    },
                    new Category
                    {
                        Name = "Thuốc kháng sinh",
                        Description = "Dùng để điều trị các bệnh do vi khuẩn gây ra như amoxicillin, azithromycin."
                    },
                    new Category
                    {
                        Name = "Thuốc tiêu hóa",
                        Description = "Hỗ trợ tiêu hóa, giảm đầy hơi, khó tiêu, đau dạ dày."
                    },
                    new Category
                    {
                        Name = "Vitamin và khoáng chất",
                        Description = "Cung cấp vitamin thiết yếu như A, B, C, D và các khoáng chất cần thiết."
                    },
                    new Category
                    {
                        Name = "Thuốc ho và cảm lạnh",
                        Description = "Dùng điều trị ho, sổ mũi, cảm lạnh thông thường."
                    }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            if (!context.Products.Any())
            {
                var products = new List<Product>
                {
                    new Product
                    {
                        ProductName = "Paracetamol 500mg",
                        ProductDescription = "Thuốc giảm đau, hạ sốt thông dụng.",
                        Image = "/images/products/Paracetamol.png",
                        Unit = "Hộp",
                        CategoryID = 1, // Thuốc giảm đau
                        MinQuantity = 10,
                        MaxQuantity = 100,
                        TotalCurrentQuantity = 50,
                        Status = true
                    },
                    new Product
                    {
                        ProductName = "Amoxicillin 500mg",
                        ProductDescription = "Kháng sinh phổ rộng nhóm penicillin.",
                        Image = "/images/products/Amoxicillin.png",
                        Unit = "Vỉ",
                        CategoryID = 2, // Thuốc kháng sinh
                        MinQuantity = 5,
                        MaxQuantity = 80,
                        TotalCurrentQuantity = 40,
                        Status = true
                    },
                    new Product
                    {
                        ProductName = "Omeprazole 20mg",
                        ProductDescription = "Điều trị trào ngược dạ dày, viêm loét.",
                        Image = "/images/products/Omeprazole.png",
                        Unit = "Lọ",
                        CategoryID = 3, // Thuốc tiêu hóa
                        MinQuantity = 10,
                        MaxQuantity = 70,
                        TotalCurrentQuantity = 30,
                        Status = true
                    },
                    new Product
                    {
                        ProductName = "Vitamin C 500mg",
                        ProductDescription = "Tăng cường sức đề kháng, chống oxy hóa.",
                        Image = "/images/products/Vitamin_C.png",
                        Unit = "Lọ",
                        CategoryID = 4, // Vitamin và khoáng chất
                        MinQuantity = 20,
                        MaxQuantity = 150,
                        TotalCurrentQuantity = 100,
                        Status = true
                    },
                    new Product
                    {
                        ProductName = "Acemol Cold & Flu",
                        ProductDescription = "Điều trị cảm cúm, ho, nghẹt mũi.",
                        Image = "/images/products/Acemol_Cold_And_Flu.png",
                        Unit = "Hộp",
                        CategoryID = 5, // Thuốc ho và cảm lạnh
                        MinQuantity = 10,
                        MaxQuantity = 60,
                        TotalCurrentQuantity = 25,
                        Status = true
                    }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

        }
    }
}
