using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.StockExportOrderDetails
{
    public class StockExportOrderDetailsRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.StockExportOrderDetails>(context), IStockExportOrderDetailsRepository
    {
    }
}
