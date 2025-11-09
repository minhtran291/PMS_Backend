using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.InventoryHistory
{
    public class InventoryHistoryRepository : RepositoryBase<PMS.Core.Domain.Entities.InventoryHistory>, IInventoryHistoryRepository
    {
        public InventoryHistoryRepository(PMSContext context) : base(context) { }
    }
}
