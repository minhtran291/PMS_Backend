using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.VnPay
{
    public sealed class VnPayInitRequestDTO
    {
        public int SalesOrderId { get; set; }
        public string PaymentType { get; set; } = "deposit";
        public string? BankCode { get; set; }
        public string? Locale { get; set; } = "vn";
    }
}
