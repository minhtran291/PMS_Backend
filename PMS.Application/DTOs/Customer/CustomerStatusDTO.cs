using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Customer
{
    public class CustomerStatusDTO
    {
        public UserStatus UserStatus { get; set; }
        public bool HasAdditionalInfo { get; set; }
        public bool NeedsAdditionalInfo { get; set; }
        public string? Message { get; set; }
    }
}

