using PMS.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Supplier
{
    public class CreateSupplierRequestDTO
    {
        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        [StringLength(200, MinimumLength = 3,
            ErrorMessage = "Tên nhà cung cấp phải từ 3 đến 200 ký tự")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(200, ErrorMessage = "Email không được vượt quá 200 ký tự")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(50, ErrorMessage = "Số điện thoại không được vượt quá 50 ký tự")]
        [RegularExpression(@"^\+?\d{8,15}$",
            ErrorMessage = "Số điện thoại chỉ gồm số, dài 8–15 ký tự, có thể bắt đầu bằng +")]
        public string? PhoneNumber { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public SupplierStatus Status { get; set; }

        [StringLength(20, ErrorMessage = "Số tài khoản không được vượt quá 20 ký tự")]
        [RegularExpression(@"^\d{8,20}$", ErrorMessage = "Số tài khoản chỉ gồm số và dài 8–20 ký tự")]
        public string? BankAccountNumber { get; set; }

        [StringLength(50, ErrorMessage = "Công nợ không được vượt quá 50 ký tự")]
        public string? MyDebt { get; set; }
    }
}
