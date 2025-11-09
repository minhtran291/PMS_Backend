using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.InventoryHistory
{
    public interface IInventoryHistoryRepository : IRepositoryBase<PMS.Core.Domain.Entities.InventoryHistory>
    {
    }
}
