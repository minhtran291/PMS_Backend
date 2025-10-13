using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.ProductCategoryRepository;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.SalesStaffProfile;
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
        ISalesStaffProfileRepository SalesStaffProfile { get; }
        ISupplierRepository Supplier { get; }
        IProductRepository Product { get; }
        IProductCategoryRepository Category { get; }
        IWarehouseRepository Warehouse { get; }
        IWarehouseLocationRepository WarehouseLocation { get; }
        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        void Dispose();
    }
}
