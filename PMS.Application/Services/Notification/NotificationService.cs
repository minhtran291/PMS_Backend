using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs.Notification;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.UnitOfWork;

namespace PMS.Application.Services.Notification
{
    public class NotificationService(IUnitOfWork unitOfWork, IMapper mapper, INotificationSender notificationSender, IUserRoleResolverService userRoleResolver) : Service(unitOfWork, mapper), INotificationService
    {

        public async Task<ServiceResult<IEnumerable<NotificationDTO>>> GetNotificationsForUserAsync(string userId)
        {
            var data = await _unitOfWork.Notification.Query()
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    SenderId = n.SenderId,
                    ReceiverId = n.ReceiverId,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            if (data == null || !data.Any())
            {
                return new ServiceResult<IEnumerable<NotificationDTO>>
                {
                    Data = [],
                    StatusCode = 200,
                    Message = "Không có thông báo."
                };
            }

            return new ServiceResult<IEnumerable<NotificationDTO>>
            {
                Data = data,
                StatusCode = 200,
                Message = "Lấy thông báo thành công."
            };
        }

        public async Task<ServiceResult<bool>> MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = _unitOfWork.Notification.Query().FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                _unitOfWork.Notification.Update(notification);
                await _unitOfWork.CommitAsync();
                return new ServiceResult<bool>
                {
                    Data = true,
                    Message = "Mark",
                    StatusCode = 200
                };
            }
            else
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    StatusCode = 200,
                    Message = "lỗi khi lấy notification"
                };
            }
        }

        public async Task SendNotificationToCustomerAsync(string senderId, string receiverId, string title, string message, NotificationType type)
        {
            var notification = new PMS.Core.Domain.Entities.Notification
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.CommitAsync();

            await notificationSender.SendNotificationAsync(receiverId, new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.CreatedAt
            });
        }

        public async Task SendNotificationToRolesAsync(
        string senderId,
        List<string> targetRoles,
        string title,
        string message,
        NotificationType type)
        {
            var targetUsers = await userRoleResolver.GetUsersByRolesAsync(targetRoles);

            foreach (var user in targetUsers)
            {
                var notification = new PMS.Core.Domain.Entities.Notification
                {
                    SenderId = senderId,
                    ReceiverId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Notification.AddAsync(notification);
                await _unitOfWork.CommitAsync();

                await notificationSender.SendNotificationAsync(user.Id, new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    notification.Type,
                    notification.CreatedAt
                });
            }
        }
    }
}
