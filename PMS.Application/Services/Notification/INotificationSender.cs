using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.Notification
{
    public interface INotificationSender
    {
        Task SendNotificationAsync(string userId, object notification);
    }
}
