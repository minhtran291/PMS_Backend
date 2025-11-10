using AutoMapper;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using PMS.Application.Services.Base;
using PMS.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.StockExportOrder
{
    public class StockExportOrderService(IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger<StockExportOrderService> logger) : Service(unitOfWork, mapper), IStockExportOderService
    {
        private readonly ILogger<StockExportOrderService> _logger = logger;
    }
}
