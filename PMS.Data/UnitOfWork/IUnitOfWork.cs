using PMS.Data.Repositories.CustomerDeptRepository;
using PMS.Data.Repositories.CustomerProfile;
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
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.Supplier;
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
        //SalesOrder
        ISalesOrderRepository SalesOrder { get; }
        ISalesOrderDetailsRepository SalesOrderDetails { get; }
        //CustomerDept
        ICustomerDeptRepository CustomerDept { get; }

        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        void Dispose();
    }
}
