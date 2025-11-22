using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class PurchasingOrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PODID { get; set; }

        [ForeignKey("Product")]
        [Required]
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string DVT { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPriceTotal { get; set; }
        public string Description { get; set; }
        public decimal Tax { get; set; }
        public int POID { get; set; }
        [ForeignKey("POID")]
        public virtual PurchasingOrder PurchasingOrder { get; set; } = null!;

        public virtual Product Product { get; set; } = null!;

        public DateTime ExpiredDate { get; set; }
    }
}
