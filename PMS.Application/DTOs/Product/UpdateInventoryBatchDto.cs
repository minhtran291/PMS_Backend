using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class UpdateInventoryBatchDto
    {      
        public required List<LotCountDto> LotCounts { get; set; }
    }
}
