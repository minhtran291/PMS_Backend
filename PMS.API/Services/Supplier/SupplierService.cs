using AutoMapper;
using PMS.API.Services.Admin;
using PMS.API.Services.Base;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.Supplier
{
    public class SupplierService(IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AdminService> logger) : Service(unitOfWork, mapper), ISupplierService
    {

    }
}
