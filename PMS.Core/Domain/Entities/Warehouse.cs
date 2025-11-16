using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public bool Status { get; set; }

        public virtual ICollection<WarehouseLocation> WarehouseLocations { get; set; } = [];
        public virtual ICollection<GoodsIssueNote> GoodsIssueNotes { get; set; } = [];
    }
}
