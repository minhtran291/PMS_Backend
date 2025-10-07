using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.Profile;
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.UnitOfWork
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IProfileRepository Profile { get; }
        ICustomerProfileRepository CustomerProfile { get; }
        IStaffProfileRepository StaffProfile { get; }
        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        void Dispose();
    }
}
