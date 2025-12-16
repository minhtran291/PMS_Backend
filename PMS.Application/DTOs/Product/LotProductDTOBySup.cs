using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class LotProductDTOBySup
    {
        public required decimal InputPrice { get; set; }
        public required string ProductName { get; set; }

        public string? ExpiredDate { get; set; }

        public int LotQuantity { get; set; }
        public int ProductID { get; set; }
        public int WarehouselocationID { get; set; }
    }
}
