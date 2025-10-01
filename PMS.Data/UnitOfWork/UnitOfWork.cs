using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.CustomerProfileRepository;
using PMS.Data.Repositories.ProfileRepository;
using PMS.Data.Repositories.StaffProfileRepository;
using PMS.Data.Repositories.UserRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.UnitOfWork
{
    public class UnitOfWork(PMSContext context,
        IUserRepository users,
        IProfileRepository profile,
        ICustomerProfileRepository customerProfile,
        IStaffProfileRepository staffProfile) : IUnitOfWork
    {
        private readonly PMSContext _context = context;

        public IUserRepository Users { get; private set; } = users;
        public IProfileRepository Profile { get; private set; } = profile;
        public ICustomerProfileRepository CustomerProfile { get; private set; } = customerProfile;
        public IStaffProfileRepository StaffProfile { get; private set; } = staffProfile;

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
