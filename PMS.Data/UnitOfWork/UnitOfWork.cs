using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.ProductCategoryRepository;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.Profile;
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.Supplier;
using PMS.Data.Repositories.User;
using PMS.Data.Repositories.Warehouse;
using PMS.Data.Repositories.WarehouseLocation;

namespace PMS.Data.UnitOfWork
{
    public class UnitOfWork(PMSContext context,
        IUserRepository users,
        IProfileRepository profile,
        ICustomerProfileRepository customerProfile,
        ISupplierRepository supplier,
        IStaffProfileRepository staffProfile,
        IProductRepository product,
        IProductCategoryRepository category,
        IWarehouseRepository warehouse,
        IWarehouseLocationRepository warehouseLocation) : IUnitOfWork
    {
        private readonly PMSContext _context = context;
        private IDbContextTransaction? _transaction;
        public IUserRepository Users { get; private set; } = users;
        public IProfileRepository Profile { get; private set; } = profile;
        public ICustomerProfileRepository CustomerProfile { get; private set; } = customerProfile;
        public IStaffProfileRepository StaffProfile { get; private set; } = staffProfile;
        public ISupplierRepository Supplier { get; private set; } = supplier;
        public IProductRepository Product { get; private set; } = product;
        public IProductCategoryRepository Category { get; private set; } = category;
        public IWarehouseRepository Warehouse { get; private set; } = warehouse;
        public IWarehouseLocationRepository WarehouseLocation { get; private set; } = warehouseLocation;

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
