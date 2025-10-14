using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.Notification
{
    public interface IUserRoleResolverService
    {
        Task<List<PMS.Core.Domain.Identity.User>> GetUsersByRolesAsync(List<string> roles);
    }
}
