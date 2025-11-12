using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum DebtStatus
    {
        BadDebt = 2, // nợ xấu
        Apart=4, // Một phần
        NoDebt=5, // hết nợ
        overTime=6 // quá hạn thanh toán
    }
}
