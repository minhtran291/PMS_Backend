using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GRN
{
    public class GRNDetailViewDTO
    {
        public int GRNDID { get; set; }
        public int ProductID { get; set; }

        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

    }
}
