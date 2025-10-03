using PMS.Core.Domain.Entities;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.CustomerProfile
{
    public interface ICustomerProfileRepository : IRepositoryBase<Core.Domain.Entities.CustomerProfile>
    {
    }
}
