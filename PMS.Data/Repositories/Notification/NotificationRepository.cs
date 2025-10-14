using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.Base;

namespace PMS.Data.Repositories.Notification
{
    public class NotificationRepository : RepositoryBase<PMS.Core.Domain.Entities.Notification>, INotificationRepository
    {
        public NotificationRepository(PMSContext context) : base(context) { }
    }
}
