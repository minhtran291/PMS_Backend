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

namespace PMS.Application.Services.RequestSalesQuotation
{
    public class RequestSalesQuotationService(IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger<RequestSalesQuotationService> logger) : Service(unitOfWork, mapper), IRequestSalesQuotationService
    {
        private readonly ILogger<RequestSalesQuotationService> _logger = logger;

        public Task CreateRequestSalesQuotation()
        {
            throw new NotImplementedException();
        }
    }
}
