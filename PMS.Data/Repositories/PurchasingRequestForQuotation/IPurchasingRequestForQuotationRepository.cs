using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.PurchasingRequestForQuotationRepository
{
    public interface IPurchasingRequestForQuotationRepository : IRepositoryBase<PurchasingRequestForQuotation>
    {
       
        Task<PurchasingRequestForQuotation> GetByIdAsync(int id, Func<IQueryable<PurchasingRequestForQuotation>, IQueryable<PurchasingRequestForQuotation>> include = null);
      

    }
}
