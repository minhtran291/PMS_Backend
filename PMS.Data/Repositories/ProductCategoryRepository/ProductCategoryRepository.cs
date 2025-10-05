using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.ProductCategoryRepository
{
    public class ProductCategoryRepository : RepositoryBase<Category>,IProductCategoryRepository
    {

        public ProductCategoryRepository(PMSContext context) : base(context) { }
    }
}
