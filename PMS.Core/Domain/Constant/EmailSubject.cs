using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Constant
{
    public class EmailSubject
    {
        public const string CONFIRM_EMAIL = "Xác nhận tài khoản";
        public const string RESET_PASSWORD = "Đặt lại mật khẩu";
        public const string SALES_QUOTATION = "Báo giá";
        public const string Invoice = "Phiếu Thu Tiền";
        public const string InvoiceLateReminder = "Nhắc nhở thanh toán hóa đơn quá hạn";
    }
}
