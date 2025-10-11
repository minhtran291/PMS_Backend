namespace PMS.API.Services.Supplier
{
    public interface ISupplierService
    {
        Task<Core.Domain.Entities.Supplier?> GetSupplierByID (string supplierId);
        Task<List<Core.Domain.Entities.Supplier>> GetAllSuppliers();

    }
}
