using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.ProductCategoryRepository;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.Profile;
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.User;

namespace PMS.Data.UnitOfWork
{
    public class UnitOfWork(PMSContext context,
        IUserRepository users,
        IProfileRepository profile,
        ICustomerProfileRepository customerProfile,
        IStaffProfileRepository staffProfile,
        IProductRepository product,
        IProductCategoryRepository category) : IUnitOfWork
    {
        private readonly PMSContext _context = context;

        public IUserRepository Users { get; private set; } = users;
        public IProfileRepository Profile { get; private set; } = profile;
        public ICustomerProfileRepository CustomerProfile { get; private set; } = customerProfile;
        public IStaffProfileRepository StaffProfile { get; private set; } = staffProfile;
        public IProductRepository Product { get; private set; } = product;
        public IProductCategoryRepository Category { get; private set; } = category;

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
