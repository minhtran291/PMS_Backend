using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class GoodReceiptNote
    {
        [Key]
        public int GRNID { get; set; }
        [Required(ErrorMessage = "Source is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        public required string Source { get; set; }
        [Required(ErrorMessage = "Create date is required.")]
        [DataType(DataType.DateTime)]
        public required DateTime CreateDate { get; set; }

        [Required(ErrorMessage = "Total is required.")]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Total { get; set; }
        [Required(ErrorMessage = "Created by is required.")]
        public required string CreateBy { get; set; }
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public int? warehouseID { get; set; }

        [ForeignKey("PurchasingOrder")]
        public int POID { get; set; }

        public virtual PurchasingOrder PurchasingOrder { get; set; } = null;

        public virtual ICollection<GoodReceiptNoteDetail> GoodReceiptNoteDetails { get; set; } = new List<GoodReceiptNoteDetail>();
    }
}
