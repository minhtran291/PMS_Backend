﻿using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.SalesQuotationValidity
{
    public class SalesQuotationValidityRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.SalesQuotationValidity>(context), ISalesQuotationValidityRepository
    {
    }
}
