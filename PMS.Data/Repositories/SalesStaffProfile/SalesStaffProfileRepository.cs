using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.SalesStaffProfile
{
    public class SalesStaffProfileRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.SalesStaffProfile>(context), ISalesStaffProfileRepository
    {
    }
}
