using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VietQR
{
    public class VietQrInitResponse
    {
        public string QrImageUrl { get; set; } = "";
        public string TransferContent { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
