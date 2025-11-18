using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.InvoiceDetailRepo
{
    public class InvoiceDetailRepository : RepositoryBase<InvoiceDetail>,IInvoiceDetailRepository
    {
        public InvoiceDetailRepository(PMSContext context) : base(context) { }
    }
}
