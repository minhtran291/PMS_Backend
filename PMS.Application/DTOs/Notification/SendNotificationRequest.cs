using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Notification
{
    public class SendNotificationRequest
    {
        [Required(ErrorMessage = "Danh sách vai trò nhận thông báo là bắt buộc.")]
        [MinLength(1, ErrorMessage = "Cần ít nhất một vai trò nhận thông báo.")]
        [Display(Name = "Danh sách vai trò nhận thông báo")]
        public List<string> TargetRoles { get; set; } = new();

        [Required(ErrorMessage = "Tiêu đề thông báo là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Tiêu đề thông báo không được vượt quá 200 ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung thông báo là bắt buộc.")]
        [StringLength(1000, ErrorMessage = "Nội dung thông báo không được vượt quá 1000 ký tự.")]
        [Display(Name = "Nội dung")]
        public string Message { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại thông báo là bắt buộc.")]
        [EnumDataType(typeof(NotificationType), ErrorMessage = "Giá trị loại thông báo không hợp lệ.")]
        [Display(Name = "Loại thông báo")]
        public NotificationType Type { get; set; } = NotificationType.System;
    }
}
