using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.DatabaseConfig
{
    public class PMSContext(DbContextOptions<PMSContext> options) : IdentityDbContext<User>(options)
    {
        public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public virtual DbSet<StaffProfile> StaffProfiles { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Warehouse> Warehouses { get; set; }
        public virtual DbSet<WarehouseLocation> WarehouseLocations { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<RequestSalesQuotation> RequestSalesQuotations { get; set; }
        public virtual DbSet<RequestSalesQuotationDetails> RequestSalesQuotationDetails { get; set; }
        public virtual DbSet<PurchasingRequestForQuotation> PurchasingRequestForQuotations { get; set; }
        public virtual DbSet<PurchasingRequestProduct> PurchasingRequestProducts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(u => u.Id);

                entity.Property(u => u.Id)
                    .IsRequired();

                entity.Property(u => u.UserName)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(u => u.RefreshToken)
                    .HasMaxLength(128);

                entity.Property(u => u.UserStatus)
                    .HasConversion<byte>()
                    .HasColumnType("TINYINT")
                    .IsRequired();

                entity.Property(u => u.FullName)
                    .HasMaxLength(128);

                entity.Property(u => u.Avatar)
                    .HasMaxLength(256);

                entity.Property(u => u.Address)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(u => u.Gender)
                    .HasColumnType("bit")
                    .IsRequired(false);

                entity.Property(u => u.PasswordHash)
                    .HasMaxLength(256);

                entity.Property(u => u.SecurityStamp)
                    .HasMaxLength(100);

                entity.Property(u => u.ConcurrencyStamp)
                    .HasMaxLength(100);

                entity.Property(u => u.PhoneNumber)
                    .HasMaxLength(16)
                    .IsRequired()
                    .IsUnicode(false);
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("Roles");

                entity.Property(r => r.ConcurrencyStamp)
                    .HasMaxLength(100);
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRole");
            });

            builder.Entity<CustomerProfile>(entity =>
            {
                entity.HasKey(cp => cp.Id);

                entity.Property(cp => cp.Id)
                    .ValueGeneratedOnAdd();

                entity.HasOne(cp => cp.User)
                    .WithOne(u => u.CustomerProfile)
                    .HasForeignKey<CustomerProfile>(cp => cp.UserId);
            });

            builder.Entity<StaffProfile>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(s => s.EmployeeCode)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.HasOne(ss => ss.User)
                    .WithOne(u => u.StaffProfile)
                    .HasForeignKey<StaffProfile>(ss => ss.UserId);
            });

            builder.Entity<Supplier>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                .ValueGeneratedOnAdd();

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Email)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.Address)
                    .IsRequired(false)
                    .HasMaxLength(300);

                entity.Property(p => p.Status)
                    .HasConversion<byte>()
                    .HasColumnType("TINYINT")
                    .IsRequired();

                entity.Property(p => p.BankAccountNumber)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(p => p.MyDebt)
                    .IsRequired(false)
                    .HasMaxLength(50);
            });

            builder.Entity<Warehouse>(entity =>
            {
                entity.HasKey(w => w.Id);

                entity.Property(w => w.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(w => w.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(w => w.Address)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(w => w.Status)
                    .HasConversion<byte>()
                    .HasColumnType("TINYINT");
            });

            builder.Entity<WarehouseLocation>(entity =>
            {
                entity.HasKey(wl => wl.Id);

                entity.Property(wl => wl.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.WarehouseId)
                    .IsRequired();

                entity.Property(e => e.RowNo)
                    .IsRequired();

                entity.Property(e => e.ColumnNo)
                      .IsRequired();

                entity.Property(e => e.LevelNo)
                      .IsRequired();

                entity.Property(e => e.Status)
                    .HasConversion<byte>()
                    .HasColumnType("TINYINT")
                    .IsRequired();

                entity.HasOne(wl => wl.Warehouse)
                    .WithMany(w => w.WarehouseLocations)
                    .HasForeignKey(wl => wl.WarehouseId);
            });

            builder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.ProductID);

                entity.Property(p => p.ProductID)
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.ProductName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.ProductDescription)
                    .HasMaxLength(300);

                entity.Property(p => p.MinQuantity)
                      .IsRequired();

                entity.Property(p => p.Unit)
                      .IsRequired();

                entity.Property(p => p.MaxQuantity)
                    .IsRequired();

                entity.Property(p => p.TotalCurrentQuantity)
                    .IsRequired();

                entity.Property(p => p.Status)
                    .IsRequired();

                entity.Property(p => p.Image);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.CategoryID);

                entity.Property(c => c.CategoryID)
                    .ValueGeneratedOnAdd();

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(c => c.Status)
                .IsRequired();
            });

            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(n => n.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(n => n.SenderId)
                    .IsRequired();

                entity.Property(n => n.ReceiverId)
                    .IsRequired();

                entity.Property(n => n.Type)
                    .HasConversion<byte>()
                    .HasColumnType("TINYINT")
                    .IsRequired();

                entity.Property(n => n.IsRead)
                    .IsRequired()
                    .HasColumnType("bit");

                entity.Property(n => n.CreatedAt)
                    .IsRequired()
                    .HasColumnType("datetime");

                entity.HasOne(n => n.Sender)
                    .WithMany(u => u.SentNotifications)
                    .HasForeignKey(n => n.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.Receiver)
                    .WithMany(u => u.ReceivedNotifications)
                    .HasForeignKey(n => n.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<RequestSalesQuotation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.RequestCode)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasConversion<byte>()
                    .HasColumnType("TINYINT")
                    .IsRequired();

                entity.HasOne(e => e.CustomerProfile)
                    .WithMany(cp => cp.RequestSalesQuotations)
                    .HasForeignKey(e => e.CustomerId);
            });

            builder.Entity<RequestSalesQuotationDetails>(entity =>
            {
                entity.HasKey(e => new { e.RequestSalesQuotationId, e.ProductId });
            });

            builder.Entity<PurchasingRequestForQuotation>(entity =>
            {
                entity.HasKey(prfq => prfq.PRFQID);

                entity.Property(prfq => prfq.RequestDate)
                    .IsRequired();

                entity.Property(prfq => prfq.TaxCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(prfq => prfq.MyPhone)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(prfq => prfq.MyAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(prfq => prfq.Status)
                    .IsRequired();

                entity.HasOne(prfq => prfq.Supplier)
                    .WithMany(s => s.PurchasingRequestForQuotations)
                    .HasForeignKey(prfq => prfq.SupplierID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(prfq => prfq.User)
                    .WithMany(u => u.PurchasingRequestForQuotations)
                    .HasForeignKey(prfq => prfq.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PurchasingRequestProduct>(entity =>
            {
                entity.HasKey(prp => prp.PRPID);

                entity.HasOne(prp => prp.PRFQ)
                    .WithMany(prfq => prfq.PRPS)
                    .HasForeignKey(prp => prp.PRFQID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(prp => prp.Product)
                    .WithMany(p => p.PRPS)
                    .HasForeignKey(prp => prp.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
