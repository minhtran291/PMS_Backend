using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using PMS.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.Supplier
{
    public class SupplierRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.Supplier>(context), ISupplierRepository
    {
    }
}
