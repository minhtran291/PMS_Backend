using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.SalesOrderDetailsRepository
{
    public class SalesOrderDetailsRepository : RepositoryBase<SalesOrderDetails>, ISalesOrderDetailsRepository
    {
        public SalesOrderDetailsRepository(PMSContext context) : base(context) { }
    }
}
