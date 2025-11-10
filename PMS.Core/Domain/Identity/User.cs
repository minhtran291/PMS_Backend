using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;

namespace PMS.Core.Domain.Identity
{
    public class User : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpriryTime { get; set; }
        public UserStatus UserStatus { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string Address { get; set; } = null!;
        public bool? Gender { get; set; }
        public DateTime CreateAt { get; set; }

        public virtual StaffProfile? StaffProfile { get; set; }
        public virtual CustomerProfile? CustomerProfile { get; set; }

        public virtual ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
        public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
        public virtual ICollection<PurchasingRequestForQuotation> PurchasingRequestForQuotations { get; set; } = new List<PurchasingRequestForQuotation>();

        public virtual ICollection<PurchasingOrder> PurchasingOrders { get; set; } = new List<PurchasingOrder>();
        public virtual ICollection<SalesQuotationComment>? SalesQuotationComments {  get; set; }
        public virtual ICollection<SalesOrder> SalesOrders { get; set; } = [];
        public virtual ICollection<StockExportOrder> StockExportOrders { get; set; } = [];
    }
}
