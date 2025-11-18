using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Data.Repositories.InvoiceRepo
{
    public class InvoiceRepository: RepositoryBase<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(PMSContext context) : base(context) { }
    }
}
