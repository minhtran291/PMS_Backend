using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.ConfigOptions
{
    public sealed class VietQRConfig
    {
        public string BankCode { get; set; } = "mbbank";
        public string AccountNumber { get; set; } = "0377708126";
        public string AccountName { get; set; } = "NGUYEN QUANG TRUNG";
        public string Template { get; set; } = "compact2";
        public string BaseImageUrl { get; set; } = "https://img.vietqr.io/image";
    }
}
