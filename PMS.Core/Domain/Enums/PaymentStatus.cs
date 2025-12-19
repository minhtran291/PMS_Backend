using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum PaymentStatus : byte
    {
        NotPaymentYet = 0, //Khách hàng chưa thanh toán bất cứ khoản nào
        Deposited = 1, //Khách hàng thanh toán số tiền cọc
        PartiallyPaid = 2, // khách trả tiền nhưng chưa đủ tổng giá trị đơn hàng
        Paid = 3, //Khách hàng thanh toán toàn bộ hoặc thanh toán đủ tiền
        Refunded = 4, //Trường hợp lỗi bên mình và khách hàng không chấp nhận thì cần trả lại tiền cho khách
        Late = 5 // khách thanh toán sau thời hạn thanh toán hóa đơn 
    }
}
