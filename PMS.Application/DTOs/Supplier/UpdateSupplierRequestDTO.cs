using PMS.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Supplier
{
    public class UpdateSupplierRequestDTO : IValidatableObject
    {
        [MaxLength(200)] public string? Name { get; set; }
        [EmailAddress, MaxLength(200)] public string? Email { get; set; }
        [RegularExpression(@"^(\d{10}|\+84\d{9})$"), MaxLength(50)] public string? PhoneNumber { get; set; }
        [MaxLength(300)] public string? Address { get; set; }

        [MaxLength(50)]
        [Range(0, 1, ErrorMessage = "Trạng thái không hợp lệ")] 
        
        public SupplierStatus Status { get; set; }
        [RegularExpression(@"^\d{8,20}$")] public string? BankAccountNumber { get; set; }
        [MaxLength(50)] public string? MyDebt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
        {
            if (Name != null && string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Tên không được rỗng", new[] { nameof(Name) });
            if (Email != null && string.IsNullOrWhiteSpace(Email))
                yield return new ValidationResult("Email không được rỗng", new[] { nameof(Email) });
            if (PhoneNumber != null && string.IsNullOrWhiteSpace(PhoneNumber))
                yield return new ValidationResult("SĐT không được rỗng", new[] { nameof(PhoneNumber) });
            if (Address != null && string.IsNullOrWhiteSpace(Address))
                yield return new ValidationResult("Địa chỉ không được rỗng", new[] { nameof(Address) });
            if (BankAccountNumber != null && string.IsNullOrWhiteSpace(BankAccountNumber))
                yield return new ValidationResult("Số TK không được rỗng", new[] { nameof(BankAccountNumber) });
            if (MyDebt != null && string.IsNullOrWhiteSpace(MyDebt))
                yield return new ValidationResult("Công nợ không được rỗng", new[] { nameof(MyDebt) });
        }
    }
}
