using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class ProductMinQuantityDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int MinQuantity { get; set; }
        public int TotalCurrentQuantity { get; set; }

        // Optional: phần trăm tồn kho
        public double PercentQuantity =>
            MinQuantity == 0 ? 0 : Math.Round((double)TotalCurrentQuantity / MinQuantity * 100, 2);
    }
}
