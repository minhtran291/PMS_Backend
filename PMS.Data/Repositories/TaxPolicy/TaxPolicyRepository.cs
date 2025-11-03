using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.TaxPolicy
{
    public class TaxPolicyRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.TaxPolicy>(context), ITaxPolicyRepository
    {
    }
}
