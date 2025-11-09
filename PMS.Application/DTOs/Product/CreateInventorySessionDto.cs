using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class CreateInventorySessionDto
    {
        public required string SessionName { get; set; }
        public required string UserId { get; set; }
        public List<LotCheckDto> LotChecks { get; set; } = new();
    }
}
