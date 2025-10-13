using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Supplier
{
    public class SupplierResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Address { get; set; } = null!;
        public SupplierStatus Status { get; set; }
        public string BankAccountNumberMasked { get; set; } = "";
        public string MyDebt { get; set; } = null!;
    }
}
