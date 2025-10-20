using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;

namespace PMS.Core.Domain.Entities
{
    public class PurchasingRequestForQuotation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PRFQID { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string TaxCode { get; set; } = string.Empty;

        [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải có đúng 10 chữ số")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string MyPhone { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string MyAddress { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public PRFQStatus Status { get; set; } = PRFQStatus.Pending;
        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; } = null!;
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<PurchasingRequestProduct> PRPS { get; set; } = new List<PurchasingRequestProduct>();
    }
}
