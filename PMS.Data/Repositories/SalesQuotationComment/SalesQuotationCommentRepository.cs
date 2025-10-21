using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.SalesQuotationComment
{
    public class SalesQuotationCommentRepository(PMSContext context) : RepositoryBase<Core.Domain.Entities.SalesQuotationComment>(context), ISalesQuotationCommentRepository
    {
    }
}
