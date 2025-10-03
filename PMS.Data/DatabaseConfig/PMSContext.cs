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
        public virtual DbSet<Profile> Profiles { get; set; }
        public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public virtual DbSet<StaffProfile> StaffProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("Roles");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRole");
            });

            builder.Entity<Profile>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                    .ValueGeneratedOnAdd();

                entity.HasOne(p => p.User)
                    .WithOne(u => u.Profile)
                    .HasForeignKey<Profile>(p => p.UserId);
            });

            builder.Entity<CustomerProfile>(entity =>
            {
                entity.HasKey(cp => cp.Id);

                entity.Property(cp => cp.Id)
                    .ValueGeneratedOnAdd();

                entity.HasOne(cp => cp.Profile)
                    .WithOne(p => p.CustomerProfile)
                    .HasForeignKey<CustomerProfile>(cp => cp.ProfileId);
            });

            builder.Entity<StaffProfile>(entity =>
            {
                entity.HasKey(sp => sp.Id);

                entity.Property(sp => sp.Id)
                    .ValueGeneratedOnAdd();

                entity.HasOne(cp => cp.Profile)
                    .WithOne(p => p.StaffProfile)
                    .HasForeignKey<StaffProfile>(cp => cp.ProfileId);
            });
        }
    }
}
