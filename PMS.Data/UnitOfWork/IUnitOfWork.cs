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
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IProfileRepository Profile { get; }
        ICustomerProfileRepository CustomerProfile { get; }
        IStaffProfileRepository StaffProfile { get; }
        Task<int> CompleteAsync();
        Task CommitAsync();
    }
}
