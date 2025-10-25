using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.DatabaseConfig;

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
                await context.Users.AddAsync(user);
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
                await context.Users.AddAsync(manager);
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
                await context.Users.AddAsync(salesStaff);
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

                var salesProfile = new StaffProfile
                {
                    UserId = salesStaff.Id,
                    EmployeeCode = "SALE-001"
                };

                await context.StaffProfiles.AddAsync(salesProfile);
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
                await context.Users.AddAsync(purchasesStaff);
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

                var purchasesProfile = new StaffProfile
                {
                    UserId = purchasesStaff.Id,
                    EmployeeCode = "PURCHASE-001"
                };

                await context.StaffProfiles.AddAsync(purchasesProfile);
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
                await context.Users.AddAsync(warehouseStaff);
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

                var warehouseProfile = new StaffProfile
                {
                    UserId = warehouseStaff.Id,
                    EmployeeCode = "WAREHOUSE-001"
                };

                await context.StaffProfiles.AddAsync(warehouseProfile);
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
                await context.Users.AddAsync(accountant);
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

                var accountantProfile = new StaffProfile
                {
                    UserId = accountant.Id,
                    EmployeeCode = "ACCOUNTANT-001"
                };

                await context.StaffProfiles.AddAsync(accountantProfile);
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


            if (!context.Suppliers.Any())
            {
                var suppliers = new List<Supplier>
                {
                    new Supplier
                    {
                        Name = "Công ty Dược Phẩm Trung Ương CPC1",
                        Email = "contact@cpc1.vn",
                        PhoneNumber = "024-38212345",
                        Address = "Số 356A Giải Phóng, Hà Nội",
                        Status = SupplierStatus.Active,
                        BankAccountNumber = "1234567890",
                        MyDebt = "0"
                    },
                    new Supplier
                    {
                        Name = "Công ty TNHH Dược Phẩm Hoa Linh",
                        Email = "info@hoalinh.vn",
                        PhoneNumber = "024-37751234",
                        Address = "Số 102 Thái Thịnh, Đống Đa, Hà Nội",
                        Status = SupplierStatus.Active,
                        BankAccountNumber = "2233445566",
                        MyDebt = "0"
                    },
                    new Supplier
                    {
                        Name = "Công ty Dược Phẩm Imexpharm",
                        Email = "support@imexpharm.vn",
                        PhoneNumber = "0277-3856789",
                        Address = "Số 4, QL30, TP. Cao Lãnh, Đồng Tháp",
                        Status = SupplierStatus.Active,
                        BankAccountNumber = "9988776655",
                        MyDebt = "0"
                    }
                };

                await context.Suppliers.AddRangeAsync(suppliers);
                await context.SaveChangesAsync();
            }

            if (!context.Warehouses.Any())
            {
                var warehouses = new List<Warehouse>
                {
                    new Warehouse
                    {
                        Name = "Kho A",
                        Address = "Số 123 Phạm Văn Đồng, Cầu Giấy, Hà Nội",
                        Status = true
                    },
                    new Warehouse
                    {
                        Name = "Kho B",
                        Address = "Số 123 Phạm Văn Đồng, Cầu Giấy, Hà Nội",
                        Status = true
                    }
                };

                await context.Warehouses.AddRangeAsync(warehouses);
                await context.SaveChangesAsync();
            }

            if (!context.WarehouseLocations.Any())
            {
                var warehouseLocations = new List<WarehouseLocation>
                {
                    new WarehouseLocation
                    {
                        WarehouseId = 1,
                        LocationName = "Khu thuốc A1",
                        Status = true,
                    },
                    new WarehouseLocation
                    {
                        WarehouseId = 1,
                        LocationName = "Khu thuốc A2",
                        Status = true,
                    },
                    new WarehouseLocation
                    {
                        WarehouseId = 2,
                        LocationName = "Khu thuốc B1",
                        Status = true,
                    },
                    new WarehouseLocation
                    {
                        WarehouseId = 2,
                        LocationName = "Khu thuốc B2",
                        Status = true,
                    },
                };

                await context.WarehouseLocations.AddRangeAsync(warehouseLocations);
                await context.SaveChangesAsync();
            }

            if (!context.LotProducts.Any())
            {
                var lotProducts = new List<LotProduct>
                {
                    new LotProduct
                    {
                        ProductID = 1,
                        SupplierID = 1,
                        InputDate = DateTime.Now.AddMonths(-2),
                        ExpiredDate = DateTime.Now.AddMonths(2),
                        LotQuantity = 1000,
                        InputPrice = 12000,
                        SalePrice = 15000,
                        WarehouselocationID = 1
                    },
                    new LotProduct
                    {
                        ProductID = 1,
                        SupplierID = 1,
                        InputDate = DateTime.Now.AddMonths(-1),
                        ExpiredDate = DateTime.Now.AddMonths(3),
                        LotQuantity = 1000,
                        InputPrice = 15000,
                        SalePrice = 18000,
                        WarehouselocationID = 2
                    },
                    new LotProduct
                    {
                        ProductID = 2,
                        SupplierID = 2,
                        InputDate = DateTime.Now.AddMonths(-2),
                        ExpiredDate = DateTime.Now.AddMonths(2),
                        LotQuantity = 800,
                        InputPrice = 25000,
                        SalePrice = 30000,
                        WarehouselocationID = 3
                    },
                    new LotProduct
                    {
                        ProductID = 2,
                        SupplierID = 2,
                        InputDate = DateTime.Now.AddMonths(-1),
                        ExpiredDate = DateTime.Now.AddMonths(3),
                        LotQuantity = 800,
                        InputPrice = 27000,
                        SalePrice = 32000,
                        WarehouselocationID = 4
                    },
                    new LotProduct
                    {
                        ProductID = 3,
                        SupplierID = 3,
                        InputDate = DateTime.Now.AddMonths(-2),
                        ExpiredDate = DateTime.Now.AddMonths(2),
                        LotQuantity = 600,
                        InputPrice = 18000,
                        SalePrice = 22000,
                        WarehouselocationID = 1
                    },
                    new LotProduct
                    {
                        ProductID = 3,
                        SupplierID = 3,
                        InputDate = DateTime.Now.AddMonths(-1),
                        ExpiredDate = DateTime.Now.AddMonths(3),
                        LotQuantity = 600,
                        InputPrice = 20000,
                        SalePrice = 24000,
                        WarehouselocationID = 2
                    },
                    new LotProduct
                    {
                        ProductID = 4,
                        SupplierID = 2,
                        InputDate = DateTime.Now.AddMonths(-2),
                        ExpiredDate = DateTime.Now.AddMonths(2),
                        LotQuantity = 1200,
                        InputPrice = 9000,
                        SalePrice = 13000,
                        WarehouselocationID = 3
                    },
                    new LotProduct
                    {
                        ProductID = 4,
                        SupplierID = 2,
                        InputDate = DateTime.Now.AddMonths(-1),
                        ExpiredDate = DateTime.Now.AddMonths(3),
                        LotQuantity = 1200,
                        InputPrice = 11000,
                        SalePrice = 15000,
                        WarehouselocationID = 4
                    },
                    new LotProduct
                    {
                        ProductID = 5,
                        SupplierID = 1,
                        InputDate = DateTime.Now.AddMonths(-2),
                        ExpiredDate = DateTime.Now.AddMonths(2),
                        LotQuantity = 500,
                        InputPrice = 20000,
                        SalePrice = 25000,
                        WarehouselocationID = 1
                    },
                    new LotProduct
                    {
                        ProductID = 5,
                        SupplierID = 1,
                        InputDate = DateTime.Now.AddMonths(-1),
                        ExpiredDate = DateTime.Now.AddMonths(3),
                        LotQuantity = 500,
                        InputPrice = 22000,
                        SalePrice = 27000,
                        WarehouselocationID = 2
                    }
                };

                await context.LotProducts.AddRangeAsync(lotProducts);
                await context.SaveChangesAsync();
            }

            if (!context.TaxPolicies.Any())
            {
                var taxPolicies = new List<TaxPolicy>
                {
                    new TaxPolicy
                    {
                        Name = "VAT 10%",
                        Rate = 0.10m,
                        Description = "Thuế giá trị gia tăng 10%",
                        Status = true
                    },
                    new TaxPolicy
                    {
                        Name = "VAT 5%",
                        Rate = 0.05m,
                        Description = "Thuế giá trị gia tăng 5%",
                        Status = true
                    },
                    new TaxPolicy
                    {
                        Name = "Không chịu thuế",
                        Rate = 0.00m,
                        Description = "Miễn áp thuế giá trị gia tăng",
                        Status = true
                    }
                };

                await context.TaxPolicies.AddRangeAsync(taxPolicies);
                await context.SaveChangesAsync();
            }

            if (!context.SalesQuotationValidities.Any())
            {
                var validities = new List<SalesQuotationValidity>
                {
                    new SalesQuotationValidity
                    {
                        Name = "Hạn 15 ngày",
                        Content = "Báo giá có hiệu lực trong 15 ngày kể từ ngày phát hành.",
                        Days = 15,
                        Status = true
                    },
                    new SalesQuotationValidity
                    {
                        Name = "Hạn 30 ngày",
                        Content = "Báo giá có hiệu lực trong 30 ngày kể từ ngày phát hành.",
                        Days = 30,
                        Status = true
                    },
                    new SalesQuotationValidity
                    {
                        Name = "Hạn 45 ngày",
                        Content = "Báo giá có hiệu lực trong 45 ngày kể từ ngày phát hành.",
                        Days = 45,
                        Status = true
                    },
                    new SalesQuotationValidity
                    {
                        Name = "Hạn 60 ngày",
                        Content = "Báo giá có hiệu lực trong 60 ngày kể từ ngày phát hành.",
                        Days = 60,
                        Status = true
                    }
                };

                await context.SalesQuotationValidities.AddRangeAsync(validities);
                await context.SaveChangesAsync();
            }

            if (!context.SalesQuotationNotes.Any())
            {
                var notes = new SalesQuotationNote
                {
                    Title = "GHI CHÚ (NOTES)",
                    Content =
@"• Hiệu lực báo giá có giá trị 30 ngày kể từ ngày báo giá
• Quá thời hạn trên, giá chào trong Bản báo giá này có thể được điều chỉnh theo thực tế
• Giá trên chưa bao gồm GTGT, chi phí vận chuyển
• Hàng hóa dự kiến giao trong thời gian 30 ngày kể từ ngày ký kết hợp đồng và chuyển tiền đợt 1
• Thanh toán bằng tiền mặt hoặc chuyển khoản vào tài khoản BBPharmacy: 3658686888 MBank

Lịch biểu thanh toán:
Đợt 1: Tạm ứng 70% sau khi ký hợp đồng
Đợt 2: Thanh toán 30% trong vòng 03 ngày kể từ khi hàng được bàn giao",
                    IsActive = true
                };

                await context.SalesQuotationNotes.AddAsync(notes);
                await context.SaveChangesAsync();
            }
        }
    }
}
