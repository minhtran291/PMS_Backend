using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.InventorySession
{
    public class InventorySessionRepository : RepositoryBase<PMS.Core.Domain.Entities.InventorySession>, IInventorySessionRepository
    {
        public InventorySessionRepository(PMSContext context) : base(context) { }
    }
}
