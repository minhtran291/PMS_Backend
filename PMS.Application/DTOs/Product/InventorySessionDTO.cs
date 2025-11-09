using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.Product
{
    public class InventorySessionDTO
    {
        public int InventorySessionID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CreatedBy { get; set; }
        public InventorySessionStatus? Status { get; set; }
    }
}
