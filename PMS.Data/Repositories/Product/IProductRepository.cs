﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.ProductRepository
{
    public interface IProductRepository : IRepositoryBase<Product>
    {
    }
}
