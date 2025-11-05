using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class PhysicalInventoryUpdateDTO
    {
        public int LotID { get; set; }
        public int RealQuantity { get; set; }
    }
}
