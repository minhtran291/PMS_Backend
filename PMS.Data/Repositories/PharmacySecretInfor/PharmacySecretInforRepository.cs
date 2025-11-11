using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.PharmacySecretInfor
{
    public class PharmacySecretInforRepository : RepositoryBase<PMS.Core.Domain.Entities.PharmacySecretInfor>, IPharmacySecretInforRepository
    {
        public PharmacySecretInforRepository(PMSContext context) : base(context) { }
    }
}
