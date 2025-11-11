using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Enums
{
    public enum DebtStatus
    {
        Maturity = 1, // đáo hạn
        BadDebt = 2, // nợ xấu
        OnTime = 3, // đúng hạn
        Apart=4, // Một phần

    }
}
