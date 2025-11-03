using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class WarehouseLocation
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public required string LocationName { get; set; }
        public bool Status { get; set; }

        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual ICollection<LotProduct> LotProducts { get; set; } = [];
    }
}
