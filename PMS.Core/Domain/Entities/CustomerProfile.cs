using PMS.Core.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class CustomerProfile
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public long? Mst { get; set; }
        public string? ImageCnkd { get; set; }
        public string? ImageByt { get; set; }
        public long? Mshkd { get; set; }
        public virtual Profile Profile { get; set; } = null!;
    }
}
