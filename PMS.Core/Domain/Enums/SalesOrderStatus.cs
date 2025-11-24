using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum SalesOrderStatus : byte
    {
        Draft = 0, //Customer tạo đơn đặt hàng nháp
        Send = 1, //Customer gửi yêu cầu đặt đơn hàng
        Approved =2, //SalesStaff chấp nhận đơn đặt hàng từ khách hàng
        Rejected = 3,//SalesStaff từ chối đơn hàng vì lý do nào đó
        ApartDelivered = 4, //Trường hợp khách 
        Delivered = 5, //Trường hợp kho đã xuất đủ hàng theo sales order
        Complete = 6, //Trong trường hợp khách đã trả đủ tiền và hàng đã được giao đủ
        NotComplete = 7, //trong trường hợp khách không lấy hàng và cần refund tiền
        BackSalesOrder = 8 //Trong trường hợp đợi hàng và khách hàng đồng ý
    }
}
