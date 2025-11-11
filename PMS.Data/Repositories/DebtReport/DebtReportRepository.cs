using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.DebtReport
{
    public class DebtReportRepository : RepositoryBase<PMS.Core.Domain.Entities.DebtReport>, IDebtReportRepository
    {
        public DebtReportRepository(PMSContext context) : base(context) { }
    }
}
