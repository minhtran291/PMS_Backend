using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;

namespace PMS.Core.DTO.Content
{
    public class RegisterUser
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public required string Email { get; set; }
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
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
        public UserStatus Status { get; set; } = UserStatus.Inactive;
        public string Role { get; set; } = UserRoles.CUSTOMER;
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}
