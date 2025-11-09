using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Core.Domain.Entities
{
    public class InventorySession
    {
        [Key]
        public int InventorySessionID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CreatedBy { get; set; }
        public InventorySessionStatus? Status { get; set; }

        public virtual ICollection<InventoryHistory> InventoryHistories { get; set; } = [];
    }
}
