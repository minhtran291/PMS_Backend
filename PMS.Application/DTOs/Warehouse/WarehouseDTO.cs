using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Warehouse
{
    public class WarehouseDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public bool Status { get; set; }
    }
}
