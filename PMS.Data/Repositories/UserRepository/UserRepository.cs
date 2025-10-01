using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Identity;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.UserRepository
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserManager<User> UserManager { get; private set; }

        public SignInManager<User> SignInManager { get; private set; }

        public UserRepository(PMSContext context, 
            UserManager<User> userManager, SignInManager<User> signInManager) : base(context)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }
    }
}
