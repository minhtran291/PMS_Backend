using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Entities
{
    public class PharmacySecretInfor
    {
        public int PMSID { get; set; } //==1

        [Column(TypeName = "decimal(18,2)")]
        public decimal Equity { get; set; } // Vốn chủ sở hữu

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRecieve { get; set; } // Tổng thu 

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPaid { get; set; } // Tổng chi

        // DebtCeiling sẽ được tạo dưới dạng Computed Column trong DB
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DebtCeiling { get; private set; }
    }
}
