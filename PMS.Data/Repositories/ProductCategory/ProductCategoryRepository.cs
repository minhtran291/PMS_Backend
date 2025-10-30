using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace PMS.Data.Repositories.ProductCategoryRepository
{
    public class ProductCategoryRepository : RepositoryBase<Category>,IProductCategoryRepository
    {

        public ProductCategoryRepository(PMSContext context) : base(context) { }

        public async Task ReseedIdentityToMaxAsync()
        {
            // Determine current MAX(CategoryID)
            var maxId = await _context.Categories.MaxAsync(c => (int?)c.CategoryID) ?? 0;
            // Reseed identity to MAX so next insert becomes MAX+1
            await _context.Database.ExecuteSqlRawAsync(
                "DBCC CHECKIDENT ('[Categories]', RESEED, {0})",
                parameters: [maxId]
            );
        }
    }
}
