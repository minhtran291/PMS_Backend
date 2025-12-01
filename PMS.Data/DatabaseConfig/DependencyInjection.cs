using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PMS.Core.Domain.Identity;
using PMS.Data.Repositories.CustomerDebtRepo;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.GoodReceiptNoteDetailRepository;
using PMS.Data.Repositories.GoodReceiptNoteRepository;
using PMS.Data.Repositories.GoodsIssueNote;
using PMS.Data.Repositories.GoodsIssueNoteDetails;
using PMS.Data.Repositories.InventoryHistory;
using PMS.Data.Repositories.InventorySession;
using PMS.Data.Repositories.LotProductRepository;
using PMS.Data.Repositories.Notification;
using PMS.Data.Repositories.ProductCategoryRepository;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.PurchasingOrderDetailRepository;
using PMS.Data.Repositories.PurchasingOrderRepository;
using PMS.Data.Repositories.PurchasingRequestForQuotationRepository;
using PMS.Data.Repositories.PurchasingRequestProductRepository;
using PMS.Data.Repositories.QuotationDetailRepository;
using PMS.Data.Repositories.QuotationRepository;
using PMS.Data.Repositories.RequestSalesQuotation;
using PMS.Data.Repositories.RequestSalesQuotationDetails;
using PMS.Data.Repositories.SalesOrderDetailsRepository;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Data.Repositories.SalesQuotation;
using PMS.Data.Repositories.SalesQuotationComment;
using PMS.Data.Repositories.SalesQuotationDetails;
using PMS.Data.Repositories.SalesQuotationNote;
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.StockExportOrder;
using PMS.Data.Repositories.StockExportOrderDetails;
using PMS.Data.Repositories.Supplier;
using PMS.Data.Repositories.TaxPolicy;
using PMS.Data.Repositories.User;
using PMS.Data.Repositories.Warehouse;
using PMS.Data.Repositories.WarehouseLocation;
using PMS.Data.UnitOfWork;
using PMS.Data.Repositories.DebtReport;
using PMS.Data.Repositories.PharmacySecretInfor;
using PMS.Data.Repositories.InvoiceRepo;
using PMS.Data.Repositories.InvoiceDetailRepo;
using PMS.Data.Repositories.PaymentRemainRepo;
using PMS.Data.Repositories.SalesOrderDepositCheck;

namespace PMS.Data.DatabaseConfig
{
    public static class DependencyInjection
    {
        public static void AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PMSContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DB"));
            });
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
            services.AddScoped<IStaffProfileRepository, StaffProfileRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            //
            services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            services.AddScoped<IWarehouseLocationRepository, WarehouseLocationRepository>();
            //
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
            services.AddScoped<ILotProductRepository, LotProductRepository>();
            //
            services.AddScoped<INotificationRepository, NotificationRepository>();
            //
            services.AddScoped<IRequestSalesQuotationRepository, RequestSalesQuotationRepository>();
            services.AddScoped<IRequestSalesQuotationDetailsRepository, RequestSalesQuotationDetailsRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            //
            services.AddScoped<IPurchasingRequestForQuotationRepository,
            PurchasingRequestForQuotationRepository>();
            services.AddScoped<IPurchasingRequestProductRepository,
            PurchasingRequestProductRepository>();
            //
            services.AddScoped<IQuotationRepository, QuotationRepository>();
            services.AddScoped<IQuotationDetailRepository, QuotationDetailRepository>();
            //
            services.AddScoped<IPurchasingOrderRepository, PurchasingOrderRepository>();
            services.AddScoped<IPurchasingOrderDetailRepository, PurchasingOrderDetailRepository>();
            //
            services.AddScoped<ISalesQuotationRepository, SalesQuotationRepository>();
            services.AddScoped<ISalesQuotationDetailsRepository, SalesQuotationDetailsRepository>();
            services.AddScoped<ISalesQuotationCommentRepository, SalesQuotationCommentRepository>();
            services.AddScoped<ITaxPolicyRepository, TaxPolicyRepository>();
            services.AddScoped<IGoodReceiptNoteRepository, GoodReceiptNoteRepository>();
            services.AddScoped<IGoodReceiptNoteDetailRepository, GoodReceiptNoteDetailRepository>();
            services.AddScoped<ISalesQuotationNoteRepository, SalesQuotationNoteRepository>();
            //
            services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
            services.AddScoped<ISalesOrderDetailsRepository, SalesOrderDetailsRepository>();
            services.AddScoped<ISalesOrderDepositCheckRepo, SalesOrderDepositCheckRepo>();
            //
            services.AddScoped<ICustomerDebtRepository, CustomerDebtRepository>();
            //
            services.AddScoped<IInventoryHistoryRepository, InventoryHistoryRepository>();
            services.AddScoped<IInventorySessionRepository, InventorySessionRepository>();
            //
            services.AddScoped<IStockExportOrderRepository, StockExportOrderRepository>();
            services.AddScoped<IStockExportOrderDetailsRepository, StockExportOrderDetailsRepository>();
            //
            services.AddScoped<IDebtReportRepository, DebtReportRepository>();
            //
            services.AddScoped<IPharmacySecretInforRepository, PharmacySecretInforRepository>();
            //
            services.AddScoped<IGoodsIssueNoteRepository, GoodsIssueNoteRepository>();
            services.AddScoped<IGoodsIssueNoteDetailsRepository, GoodsIssueNoteDetailsRepository>();
            //
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IInvoiceDetailRepository, InvoiceDetailRepository>();
            services.AddScoped<IPaymentRemainRepository, PaymentRemainRepository>();

        }

        public static void AddIdentityConfig(this IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;

            })
                .AddEntityFrameworkStores<PMSContext>()
                .AddDefaultTokenProviders();
        }
    }
}
