using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.Notification;
using PMS.Data.Repositories.ProductCategoryRepository;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.Supplier;
using PMS.Data.Repositories.User;
using PMS.Data.Repositories.Warehouse;
using PMS.Data.Repositories.WarehouseLocation;

namespace PMS.Data.UnitOfWork
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        ICustomerProfileRepository CustomerProfile { get; }
        IStaffProfileRepository StaffProfile { get; }
        ISupplierRepository Supplier { get; }
        IProductRepository Product { get; }
        IProductCategoryRepository Category { get; }
        IWarehouseRepository Warehouse { get; }
        INotificationRepository Notification { get; }
        IWarehouseLocationRepository WarehouseLocation { get; }
        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        void Dispose();
    }
}
