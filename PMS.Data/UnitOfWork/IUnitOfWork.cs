using PMS.Data.Repositories.CustomerDebtRepo;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.GoodReceiptNoteDetailRepository;
using PMS.Data.Repositories.GoodReceiptNoteRepository;
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

namespace PMS.Data.UnitOfWork
{
    public interface IUnitOfWork
    {
        //Users
        IUserRepository Users { get; }
        ICustomerProfileRepository CustomerProfile { get; }
        IStaffProfileRepository StaffProfile { get; }
        ISupplierRepository Supplier { get; }
        //Product
        IProductRepository Product { get; }
        IProductCategoryRepository Category { get; }
        ILotProductRepository LotProduct { get; }
        //Warehouse
        IWarehouseRepository Warehouse { get; }
        IWarehouseLocationRepository WarehouseLocation { get; }
        //Notification
        INotificationRepository Notification { get; }
        //RequestSalesQuotation
        IRequestSalesQuotationRepository RequestSalesQuotation { get; }
        IRequestSalesQuotationDetailsRepository RequestSalesQuotationDetails { get; }
        //PurchasingRequestForQuotation
        IPurchasingRequestForQuotationRepository PurchasingRequestForQuotation { get; }
        IPurchasingRequestProductRepository PurchasingRequestProduct { get; }
        //Quotation
        IQuotationRepository Quotation { get; }
        IQuotationDetailRepository QuotationDetail { get; }
        //PurchasingOrder
        IPurchasingOrderRepository PurchasingOrder { get; }
        IPurchasingOrderDetailRepository PurchasingOrderDetail { get; }
        //Sales Quotation
        ISalesQuotationRepository SalesQuotation { get; }
        ISalesQuotationDetailsRepository SalesQuotationDetails { get; }
        //
        ISalesQuotationCommentRepository SalesQuotationComment { get; }
        ITaxPolicyRepository TaxPolicy { get; }
        //GoodReceiptNote
        IGoodReceiptNoteRepository GoodReceiptNote { get; }
        IGoodReceiptNoteDetailRepository GoodReceiptNoteDetail { get; }
        ISalesQuotationNoteRepository SalesQuotationNote { get; }
        //SalesOrder
        ISalesOrderRepository SalesOrder { get; }
        ISalesOrderDetailsRepository SalesOrderDetails { get; }
        //CustomerDept
        ICustomerDebtRepository CustomerDebt { get; }
        //InventoryHistory
        IInventoryHistoryRepository InventoryHistory { get; }
        //InventorySession
        IInventorySessionRepository InventorySession { get; }
        //StockExportOrder
        IStockExportOrderRepository StockExportOrder { get; }
        IStockExportOrderDetailsRepository StockExportOrderDetails { get; }

        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        void Dispose();
    }
}
