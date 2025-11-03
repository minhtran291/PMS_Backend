using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesQuotation
{
    public class LotDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit {  get; set; } = string.Empty;
        public int? LotID { get; set; }
        public DateTime? InputDate { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public int? LotQuantity { get; set; }
        public string? Note {  get; set; }
    }
}
