using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.DTO.Content
{
    public class GRNDManuallyDTO : IGRNDetail
    {
        public int ProductID { get; set; }
        [Required(ErrorMessage = "Unit price is required.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative.")]
        public decimal UnitPrice { get; set; }
        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
        [Required(ErrorMessage = "ExpiredDate is required.")]
        public DateTime ExpiredDate { get; set; }
        //public virtual GRNManuallyDTO GRNManuallyDTO { get; set; } = null!;
    }
}
