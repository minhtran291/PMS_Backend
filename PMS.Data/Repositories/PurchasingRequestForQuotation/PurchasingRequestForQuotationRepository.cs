using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.PurchasingRequestForQuotationRepository
{
    public class PurchasingRequestForQuotationRepository : RepositoryBase<PurchasingRequestForQuotation>, IPurchasingRequestForQuotationRepository
    {
        public PurchasingRequestForQuotationRepository(PMSContext context) : base(context) { }
        public async Task<PurchasingRequestForQuotation> GetByIdAsync(int id, Func<IQueryable<PurchasingRequestForQuotation>, IQueryable<PurchasingRequestForQuotation>> include = null)
        {
            IQueryable<PurchasingRequestForQuotation> query = _context.PurchasingRequestForQuotations;
            if (include != null)
            {
                query = include(query);
            }
            return await query.FirstOrDefaultAsync(p => p.PRFQID == id);
        }

    }
}
