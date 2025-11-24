using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum VNPayStatus : byte
    {
        Pending = 0, //Đợi thanh toán
        Success = 1, //Khách hàng thanh toán thành công qua VNPay
        Failed = 2 //Khách hàng thanh toán thất bại 
    }
}
