using Microsoft.AspNetCore.SignalR;
using PMS.Application.Services.Notification;

namespace PMS.API
{
    public class SignalRNotificationSender : INotificationSender
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        public SignalRNotificationSender(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task SendNotificationAsync(string userId, object notification)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }
    }
}
