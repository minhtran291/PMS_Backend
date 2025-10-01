using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Identity;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.UserRepository
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        UserManager<User> UserManager { get; }

        SignInManager<User> SignInManager { get; }
    }
}
