using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Content
{
    public interface IGRNDetail
    {
        int ProductID { get; set; }
        decimal UnitPrice { get; set; }
        int Quantity { get; set; }
        DateTime ExpiredDate { get; set; }
        decimal TaxPro { get; set; }
    }
}
