using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Invoice
{
    public class SmartCASignInvoiceRequestDTO
    {
        // MST/CCCD của thuê bao SmartCA (pharmacy)
        public string UserId { get; set; } = default!;
        // Mật khẩu đăng nhập SmartCA
        public string Password { get; set; } = default!;
        // OTP hiện trên app SmartCA
        public string Otp { get; set; } = default!;
    }
}
