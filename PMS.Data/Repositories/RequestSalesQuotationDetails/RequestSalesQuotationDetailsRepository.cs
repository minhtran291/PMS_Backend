using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.RequestSalesQuotationDetails
{
    public class RequestSalesQuotationDetailsRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.RequestSalesQuotationDetails>(context), IRequestSalesQuotationDetailsRepository
    {
    }
}
