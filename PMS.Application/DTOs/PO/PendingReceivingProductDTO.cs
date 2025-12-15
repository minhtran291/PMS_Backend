using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class PendingReceivingProductDTO
    {
        public int POID { get; set; }

        public int ProductID { get; set; }
        public string? ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        public int OrderedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public int RemainingQuantity { get; set; }

        public DateTime ExpiredDate { get; set; }   
    }
}
