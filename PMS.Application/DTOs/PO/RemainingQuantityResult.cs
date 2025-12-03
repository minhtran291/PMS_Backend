using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class RemainingQuantityResult
    {
        public int Ordered { get; set; }
        public int Received { get; set; }
        public int Remaining => Math.Max(Ordered - Received, 0);
    }
}
