using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class WarehouseLocationDTO
    {
        public int Id { get; set; }
        public required string LocationName { get; set; }
        public bool Status { get; set; }
    }
}
