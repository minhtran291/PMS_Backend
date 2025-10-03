using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Identity;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.User
{
    public class UserRepository(PMSContext context,
        UserManager<Core.Domain.Identity.User> userManager, 
        SignInManager<Core.Domain.Identity.User> signInManager) : RepositoryBase<Core.Domain.Identity.User>(context), IUserRepository
    {
        public UserManager<Core.Domain.Identity.User> UserManager { get; private set; } = userManager;

        public SignInManager<Core.Domain.Identity.User> SignInManager { get; private set; } = signInManager;
    }
}
