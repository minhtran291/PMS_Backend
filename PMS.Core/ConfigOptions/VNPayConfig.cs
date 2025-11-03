using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VNPay
{
    public sealed class VNPayConfig
    {
        public string TmnCode { get; set; } = null!;
        public string HashSecret { get; set; } = null!;
        public string VnpUrl { get; set; } = null!;
        public string ReturnUrl { get; set; } = null!;
        public string IpnDebugDomain { get; set; } = null!;
    }
}
