using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class SupplierLotGroupDTO
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set;} = string.Empty;
        public List<LotDTO> Lots { get; set; } = [];
    }
}
