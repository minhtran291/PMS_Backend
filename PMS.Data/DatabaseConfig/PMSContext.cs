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
        //User
        public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public virtual DbSet<StaffProfile> StaffProfiles { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        //Product
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<LotProduct> LotProducts { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        //Warehouse
        public virtual DbSet<Warehouse> Warehouses { get; set; }
        public virtual DbSet<WarehouseLocation> WarehouseLocations { get; set; }
        //Notification
        public virtual DbSet<Notification> Notifications { get; set; }
        //RequestSalesQuotation
        public virtual DbSet<RequestSalesQuotation> RequestSalesQuotations { get; set; }
        public virtual DbSet<RequestSalesQuotationDetails> RequestSalesQuotationDetails { get; set; }
        //PurchasingRequestForQuotation
        public virtual DbSet<PurchasingRequestForQuotation> PurchasingRequestForQuotations { get; set; }
        public virtual DbSet<PurchasingRequestProduct> PurchasingRequestProducts { get; set; }
        //PurchasingOrder
        public virtual DbSet<PurchasingOrder> PurchasingOrders { get; set; }
        public virtual DbSet<PurchasingOrderDetail> PurchasingOrderDetails { get; set; }
        //Quotation
        public virtual DbSet<Quotation> Quotations { get; set; }
        public virtual DbSet<QuotationDetail> QuotationDetails { get; set; }
        // Sales Quotation
        public virtual DbSet<SalesQuotation> SalesQuotations { get; set; }
        public virtual DbSet<SalesQuotaionDetails> SalesQuotaionDetails { get; set; }
        //
        public virtual DbSet<SalesQuotationComment> SalesQuotationComments { get; set; }
        public virtual DbSet<TaxPolicy> TaxPolicies { get; set; }
        //GoodReceiptNote
        public virtual DbSet<GoodReceiptNote> GoodReceiptNotes { get; set; }
        public virtual DbSet<GoodReceiptNoteDetail> GoodReceiptNoteDetails { get; set; }
        public virtual DbSet<SalesQuotationNote> SalesQuotationNotes { get; set; }
        //SalesOrder
        public virtual DbSet<SalesOrder> SalesOrders { get; set; }
        public virtual DbSet<SalesOrderDetails> SalesOrderDetails { get; set; }
        public virtual DbSet<CustomerDept> CustomerDepts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            //

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
            //

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
            });

            builder.Entity<WarehouseLocation>(entity =>
            {
                entity.HasKey(wl => wl.Id);

                entity.Property(wl => wl.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.WarehouseId)
                    .IsRequired();

                entity.Property(e => e.LocationName)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.HasOne(wl => wl.Warehouse)
                    .WithMany(w => w.WarehouseLocations)
                    .HasForeignKey(wl => wl.WarehouseId);
            });
            //

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

                entity.Property(p => p.Image).IsRequired();

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LotProduct>(entity =>
            {
                entity.HasKey(lp => lp.LotID);

                entity.Property(lp => lp.InputDate)
                    .IsRequired()
                    .HasColumnType("date");

                entity.Property(lp => lp.ExpiredDate)
                    .IsRequired()
                    .HasColumnType("date");

                entity.Property(lp => lp.LotQuantity)
                    .IsRequired();

                entity.Property(lp => lp.InputPrice)
                    .HasColumnType("decimal(18,2)").IsRequired();

                entity.Property(lp => lp.SalePrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(lp => lp.lastedUpdate).HasColumnType("date");
                entity.Property(lp => lp.inventoryBy);

                entity.HasOne(lp => lp.Product)
                    .WithMany(p => p.LotProducts)
                    .HasForeignKey(lp => lp.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(lp => lp.Supplier)
                    .WithMany(s => s.LotProducts)
                    .HasForeignKey(lp => lp.SupplierID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(lp => lp.WarehouseLocation)
                    .WithMany(w => w.LotProducts)
                    .HasForeignKey(lp => lp.WarehouselocationID)
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
            //

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
            //

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
                entity.HasKey(rsqd => new { rsqd.RequestSalesQuotationId, rsqd.ProductId });

                entity.HasOne(rsqd => rsqd.RequestSalesQuotation)
                    .WithMany(rsq => rsq.RequestSalesQuotationDetails)
                    .HasForeignKey(rsqd => rsqd.RequestSalesQuotationId);

                entity.HasOne(rsqd => rsqd.Product)
                    .WithMany(p => p.RequestSalesQuotationDetails)
                    .HasForeignKey(rsqd => rsqd.ProductId);
            });
            //

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

                entity.HasOne(prfq => prfq.Quotation)
                        .WithOne(q => q.PurchasingRequestForQuotation)
                        .HasForeignKey<Quotation>(q => q.PRFQID)
                        .OnDelete(DeleteBehavior.Cascade);
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
            //

            builder.Entity<PurchasingOrder>(entity =>
            {
                entity.HasKey(po => po.POID);

                entity.Property(po => po.Total)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(po => po.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(po => po.Status);
                entity.Property(po => po.Debt).HasColumnType("decimal(18,2)");
                entity.Property(po => po.PaymentDate);
                entity.Property(po => po.Deposit).HasColumnType("decimal(18,2)");

                entity.Property(po => po.OrderDate).IsRequired();

                entity.HasOne(po => po.User)
                    .WithMany(u => u.PurchasingOrders)
                    .HasForeignKey(po => po.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(po => po.Quotations)
                    .WithMany(q => q.PurchasingOrders)
                    .HasForeignKey(po => po.QID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PurchasingOrderDetail>(entity =>
            {
                entity.HasKey(pod => pod.PODID);

                entity.Property(pod => pod.ProductID).IsRequired();

                entity.Property(pod => pod.ProductName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(pod => pod.DVT)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(pod => pod.Quantity)
                    .IsRequired();

                entity.Property(pod => pod.UnitPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(pod => pod.UnitPriceTotal)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(pod => pod.Description)
                    .HasMaxLength(500);

                entity.Property(pod => pod.ExpiredDate)
                .HasColumnType("date").IsRequired();

                entity.HasOne(pod => pod.PurchasingOrder)
                    .WithMany(po => po.PurchasingOrderDetails)
                    .HasForeignKey(pod => pod.POID)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            //

            builder.Entity<Quotation>(entity =>
            {
                entity.HasKey(q => q.QID);


                entity.Property(q => q.QID)
                    .ValueGeneratedNever();

                entity.Property(q => q.SendDate)
                    .IsRequired();


                entity.Property(q => q.QuotationExpiredDate)
                    .IsRequired();

                entity.Property(q => q.SupplierID)
                    .IsRequired();


                entity.Property(q => q.Status)
                    .IsRequired();

                entity.Property(q => q.PRFQID);
                   


                entity.HasMany(q => q.PurchasingOrders)
                    .WithOne(po => po.Quotations)
                    .HasForeignKey(po => po.QID)
                    .OnDelete(DeleteBehavior.Restrict);


                entity.HasMany(q => q.QuotationDetails)
                    .WithOne(qd => qd.Quotation)
                    .HasForeignKey(qd => qd.QID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<QuotationDetail>(entity =>
            {
                entity.HasKey(qd => qd.QDID);

                entity.Property(qd => qd.QDID)
                    .ValueGeneratedOnAdd();

                entity.Property(qd => qd.QID)
                    .IsRequired();

                entity.Property(qd => qd.ProductID)
                    .IsRequired();

                entity.Property(qd => qd.ProductName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(qd => qd.ProductDescription)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(qd => qd.ProductUnit)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(qd => qd.UnitPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(qd => qd.ProductDate)
                    .HasColumnType("date")
                    .IsRequired();

                entity.HasOne(qd => qd.Quotation)
                    .WithMany(q => q.QuotationDetails)
                    .HasForeignKey(qd => qd.QID)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            //

            builder.Entity<SalesQuotation>(entity =>
            {
                entity.HasKey(sq => sq.Id);

                entity.Property(sq => sq.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(sq => sq.RsqId)
                    .IsRequired();

                entity.Property(sq => sq.QuotationCode)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(sq => sq.Status)
                    .HasConversion<byte>()
                    .HasColumnType("TINYINT")
                    .IsRequired();

                entity.Property(sq => sq.Notes)
                    .HasMaxLength(512);

                entity.Property(sq => sq.DepositPercent)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(sq => sq.DepositDueDays)
                    .IsRequired();

                entity.HasOne(sq => sq.RequestSalesQuotation)
                    .WithMany(rsq => rsq.SalesQuotations)
                    .HasForeignKey(sq => sq.RsqId);

                entity.HasOne(sq => sq.StaffProfile)
                    .WithMany(sp => sp.SalesQuotations)
                    .HasForeignKey(sq => sq.SsId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sq => sq.SalesQuotationNote)
                    .WithMany(sqn => sqn.SalesQuotations)
                    .HasForeignKey(sq => sq.SqnId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SalesQuotaionDetails>(entity =>
            {
                entity.HasKey(sqd => sqd.Id);

                entity.Property(sqd => sqd.Id)
                    .ValueGeneratedOnAdd();

                entity.HasOne(sqd => sqd.TaxPolicy)
                    .WithMany(tp => tp.SalesQuotaionDetails)
                    .HasForeignKey(sqd => sqd.TaxId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sqd => sqd.SalesQuotation)
                    .WithMany(sq => sq.SalesQuotaionDetails)
                    .HasForeignKey(sqd => sqd.SqId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sqd => sqd.LotProduct)
                    .WithMany(lp => lp.SalesQuotaionDetails)
                    .HasForeignKey(sqd => sqd.LotId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(sqd => sqd.Product)
                    .WithMany(p => p.SalesQuotaionDetails)
                    .HasForeignKey(sqd => sqd.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(sqd => sqd.Note)
                    .HasMaxLength(500);
            });
            //

            builder.Entity<SalesQuotationComment>(entity =>
            {
                entity.HasKey(sqc => sqc.Id);

                entity.Property(sqc => sqc.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(sqc => sqc.UserId)
                    .HasMaxLength(450);

                entity.Property(sqc => sqc.Content)
                    .HasMaxLength(512);

                entity.HasOne(sqc => sqc.SalesQuotation)
                    .WithMany(sq => sq.SalesQuotationComments)
                    .HasForeignKey(sqc => sqc.SqId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sqc => sqc.User)
                    .WithMany(u => u.SalesQuotationComments)
                    .HasForeignKey(sqc => sqc.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TaxPolicy>(entity =>
            {
                entity.HasKey(tp => tp.Id);

                entity.Property(tp => tp.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(tp => tp.Name)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(tp => tp.Rate)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(tp => tp.Description)
                    .HasMaxLength(512);
            });

            //
            builder.Entity<GoodReceiptNote>(entity =>
            {
                entity.HasKey(grn => grn.GRNID);

                entity.Property(grn => grn.Source)
                    .IsRequired();

                entity.Property(grn => grn.CreateDate)
                    .IsRequired()
                    .HasColumnType("datetime");

                entity.Property(grn => grn.Total)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(grn => grn.CreateBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(grn => grn.Description)
                    .HasMaxLength(500);

                entity.Property(grn => grn.warehouseID);


                entity.HasOne(grn => grn.PurchasingOrder)
                    .WithMany(grn => grn.GoodReceiptNotes)
                    .HasForeignKey(grn => grn.POID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(grn => grn.GoodReceiptNoteDetails)
                    .WithOne(grnd => grnd.GoodReceiptNote)
                    .HasForeignKey(grnd => grnd.GRNID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<GoodReceiptNoteDetail>(entity =>
            {
                entity.HasKey(grnd => grnd.GRNDID);

                entity.Property(grnd => grnd.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(grnd => grnd.Quantity)
                    .IsRequired();

                entity.HasOne(grnd => grnd.GoodReceiptNote)
                    .WithMany(grn => grn.GoodReceiptNoteDetails)
                    .HasForeignKey(grnd => grnd.GRNID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(grnd => grnd.Product)
                    .WithMany(p => p.GoodReceiptNoteDetails)
                    .HasForeignKey(grnd => grnd.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SalesQuotationNote>(entity =>
            {
                entity.HasKey(sqn => sqn.Id);

                entity.Property(sqn => sqn.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(sqn => sqn.Title)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(sqn => sqn.Content)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();
            });

            builder.Entity<SalesOrder>(entity =>
            {
                entity.HasKey(so => so.OrderId);
                
                entity.Property(so => so.OrderId)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(so => so.SalesQuotationId)
                    .IsRequired();

                entity.Property(so => so.CustomerId)
                    .IsRequired();

                entity.Property(so => so.CreateBy)
                    .IsRequired();

                entity.Property(so => so.CreateAt)
                    .HasDefaultValueSql("GETDATE()");
                    
                entity.Property(so => so.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(so => so.DepositAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(so => so.OrderTotalPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                //1 - n (1 SalesOrder to n SalesOrderDetails)
                entity.HasMany(so => so.SalesOrderDetails)
                    .WithOne(d => d.SalesOrder)
                    .HasForeignKey(d => d.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                //1 - n (1 SalesOrder to n CustomerDepts)
                entity.HasMany(so => so.CustomerDepts)
                    .WithOne(cd => cd.SalesOrder)
                    .HasForeignKey(cd => cd.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SalesOrderDetails>(entity =>
            {
                entity.HasKey(sod => sod.SalesOrderDetailsId);

                entity.Property(sod => sod.SalesOrderDetailsId)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(sod => sod.SalesOrderId)
                    .IsRequired();

                entity.Property(sod => sod.LotId)
                    .IsRequired();

                entity.Property(sod => sod.Quantity)
                    .HasColumnType("decimal(18,1)")
                    .IsRequired();

                entity.Property(sod => sod.UnitPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.HasOne(d => d.SalesOrder)
                    .WithMany(o => o.SalesOrderDetails)
                    .HasForeignKey(d => d.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Lot)
                      .WithMany(l => l.SalesOrderDetails) 
                      .HasForeignKey(d => d.LotId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CustomerDept>(entity =>
            {
                entity.HasKey(cd => cd.Id);  

                entity.Property(cd => cd.SalesOrderId)       
                      .IsRequired();  

                entity.Property(cd => cd.CustomerId)
                      .IsRequired();     

                entity.Property(cd => cd.DeptAmount)
                      .HasColumnType("decimal(18,2)")                            
                      .IsRequired();

                //n CustomerDept thuoc ve 1 SalesOrder
                entity.HasOne(cd => cd.SalesOrder)
                    .WithMany(o => o.CustomerDepts)
                    .HasForeignKey(cd => cd.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
