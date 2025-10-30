using Microsoft.AspNetCore.Identity;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMS.Core.Domain.Identity
{
    public class User : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpriryTime { get; set; }
        public UserStatus UserStatus { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
        public bool? Gender { get; set; }
        public DateTime CreateAt { get; set; }
        
        public virtual StaffProfile? StaffProfile {  get; set; }
        public virtual CustomerProfile? CustomerProfile { get; set; }

        public virtual ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
        public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
        public virtual ICollection<PurchasingRequestForQuotation> PurchasingRequestForQuotations { get; set; } = new List<PurchasingRequestForQuotation>();
    }
}
