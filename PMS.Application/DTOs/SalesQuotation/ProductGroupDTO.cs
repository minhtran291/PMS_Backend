using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class ProductGroupDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductUnit {  get; set; } = string.Empty;
        public List<SupplierLotGroupDTO> SupplierLots { get; set; } = [];
    }
}
