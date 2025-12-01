using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.SalesOrderDepositCheck
{
    public class SalesOrderDepositCheckRepo : RepositoryBase<PMS.Core.Domain.Entities.SalesOrderDepositCheck>, ISalesOrderDepositCheckRepo
    {
        public SalesOrderDepositCheckRepo(PMSContext context) : base(context) { }
    }
}
