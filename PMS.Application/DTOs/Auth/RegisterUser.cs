using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Auth
{
    public class RegisterUser
    {
        [Required(ErrorMessage = "UserName không được để trống.")]
        [RegularExpression(@"^[a-zA-Z0-9\-._@+]+$", ErrorMessage = "UserName chỉ được chứa chữ cái, số và các ký tự: - . _ @ +")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "UserName tối đa 20 ký tự.")]
        [Display(Name = "Tên đăng nhập")]
        public required string UserName { get; set; }
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public required string ConfirmPassword { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại VN+84 10 ký tự")]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public required string Address { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Họ tên tối đa 20 ký tự.")]
        public required string FullName { get; set; }
    }
}
