using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;

namespace PMS.Application.Services.Notification
{
    public interface INotificationService
    {
        Task SendNotificationToRolesAsync(
        string senderId,
        List<string> targetRoles,
        string title,
        string message,
        NotificationType type);
        Task<ServiceResult<IEnumerable<PMS.Application.DTOs.Notification.NotificationDTO>>> GetNotificationsForUserAsync(string userId);
        Task<ServiceResult<bool>> MarkNotificationAsReadAsync(int notificationId);

        Task SendNotificationToCustomerAsync(
        string senderId,
        string receiverId,
        string title,
        string message,
        NotificationType type);

    }
}
