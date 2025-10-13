using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Supplier
{
    public class CreateSupplierRequestDTO
    {
        [Required, MaxLength(200)] public string Name { get; set; } = null!;
        [Required, EmailAddress, MaxLength(200)] public string Email { get; set; } = null!;
        [Required, RegularExpression(@"^(\d{10}|\+84\d{9})$",
           ErrorMessage = "SĐT 10 số hoặc dạng +84xxxxxxxxx")]
        [MaxLength(50)] public string PhoneNumber { get; set; } = null!;
        [Required, MaxLength(300)] public string Address { get; set; } = null!;
        [Required, MaxLength(50)] public string Status { get; set; } = "Active";
        [Required, RegularExpression(@"^\d{8,20}$")] public string BankAccountNumber { get; set; } = null!;
        [Required, MaxLength(50)] public string MyDebt { get; set; } = null!;
    }
}
