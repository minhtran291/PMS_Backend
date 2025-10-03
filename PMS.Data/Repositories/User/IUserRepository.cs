using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Identity;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.User
{
    public interface IUserRepository : IRepositoryBase<Core.Domain.Identity.User>
    {
        UserManager<Core.Domain.Identity.User> UserManager { get; }

        SignInManager<Core.Domain.Identity.User> SignInManager { get; }
    }
}
