using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VNPay
{
    public sealed class VNPayConfig
    {
        public string TmnCode { get; set; } = "";
        public string HashSecret { get; set; } = "";
        public string VnpUrl { get; set; } = "";
        public string ReturnUrl { get; set; } = "";
        public string IpnDebugDomain { get; set; } = "";
    }
}
