using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class QuotationDetail
    {
        [Key]
        public int QDID { get; set; }
        [ForeignKey("Quotation")]
        public required int QID { get; set; }
        public required int ProductID { get; set; }

        public required string ProductName { get; set; }
        public required string ProductDescription { get; set; }
        public required string ProductUnit { get; set; }
        public required decimal UnitPrice { get; set; }
        public required DateTime ProductDate { get; set; }

        public decimal Tax { get; set; }

        public virtual Quotation Quotation { get; set; } = null;
    }
}
