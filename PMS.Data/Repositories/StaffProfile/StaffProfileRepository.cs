using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.StaffProfile
{
    public class StaffProfileRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.StaffProfile>(context), IStaffProfileRepository
    {
    }
}
