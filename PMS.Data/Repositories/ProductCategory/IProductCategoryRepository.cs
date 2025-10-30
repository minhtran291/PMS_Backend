using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.ProductCategoryRepository
{
    public interface IProductCategoryRepository :  IRepositoryBase<Category>
    {
        Task ReseedIdentityToMaxAsync();
    }
}
