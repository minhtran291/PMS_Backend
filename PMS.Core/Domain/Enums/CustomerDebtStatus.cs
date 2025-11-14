using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum CustomerDebtStatus : byte
    {
        UnPaid = 0, //Chưa trả nợ
        Apart = 1, // Một phần
        NoDebt = 2, // hết nợ
        BadDebt = 3, // nợ xấu
        OverTime = 4, // quá hạn thanh toán
        Disable = 5 // trong trường hợp đơn hàng bị reject
    }
}
