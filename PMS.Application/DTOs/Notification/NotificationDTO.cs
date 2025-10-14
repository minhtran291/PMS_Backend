using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Notification
{
    public class NotificationDTO
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public string SenderId { get; set; } = string.Empty; // UserId của người gửi
        public string ReceiverId { get; set; } = string.Empty; // UserId của người nhận
        public NotificationType Type { get; set; } = NotificationType.System;

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
