using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class LotProduct
    {
        [Key]
        public int LotID { get; set; }

        public DateTime InputDate { get; set; }

        public decimal SalePrice { get; set; } = 0;

        public required decimal InputPrice { get; set; }


        public DateTime ExpiredDate { get; set; }

        public int LotQuantity { get; set; }
        public int Diff {  get; set; }
        [ForeignKey("Supplier")]
        public int SupplierID { get; set; }
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        [ForeignKey("WarehouseLocation")]
        public  int WarehouselocationID { get; set; }

        public string? note {  get; set; }
        public DateTime lastedUpdate {  get; set; }
        public string? inventoryBy {  get; set; }
        public virtual Product Product { get; set; } = null!;
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual ICollection<SalesQuotaionDetails> SalesQuotaionDetails { get; set; } = [];
        public virtual WarehouseLocation WarehouseLocation { get; set; } = null!;
    }
}
