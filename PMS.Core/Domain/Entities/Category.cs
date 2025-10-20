using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; } = false;
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
