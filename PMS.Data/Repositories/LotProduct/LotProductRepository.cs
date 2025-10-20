using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Migrations;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.LotProductRepository
{
    public class LotProductRepository : RepositoryBase<LotProduct>, ILotProductRepository
    {
        public LotProductRepository(PMSContext context) : base(context) { }
    }
}
