using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class InventoryHistory
    {
        [Key]
        public int InventoryHistoryID { get; set; }
        [ForeignKey("LotProduct")]
        public int LotID { get; set; }
        [ForeignKey("InventorySession")]
        public int InventorySessionID { get; set; }

        public string? Note { get; set; }
        public int SystemQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public int Diff => ActualQuantity - SystemQuantity;
        public DateTime LastUpdated { get; set; }
        public string? InventoryBy { get; set; }
        public virtual LotProduct LotProduct { get; set; } = null;
        public virtual InventorySession InventorySession { get; set; } = null!;
    }
}
