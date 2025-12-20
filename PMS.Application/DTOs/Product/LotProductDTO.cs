using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class LotProductDTO2
    {
        public int LotID { get; set; }

        public string? InputDate { get; set; }

        public decimal SalePrice { get; set; } = 0;

        public required decimal InputPrice { get; set; }
        public required string ProductName { get; set; }  
        public string? ExpiredDate { get; set; }

        public int LotQuantity { get; set; }


        public int SupplierID { get; set; }

        public int ProductID { get; set; }

        

        public int WarehouselocationID { get; set; }
        public string warehouseName { get; set; }
        public string warehouseName2 { get; set; }

        public DateTime LastCheckedDate { get; set; }
    }
}
