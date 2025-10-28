using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.SQ
{
    public class QuotationDTO
    {
        public required int QID { get; set; }

        [Required(ErrorMessage = "Ngày yêu cầu không được phép bỏ trống")]
        public required DateTime SendDate { get; set; }

        [Required(ErrorMessage = "SupplierID không được phép bỏ trống")]
        public required int SupplierID { get; set; }

        [Required(ErrorMessage = "Trạng thái không được phép bỏ trống")]
        public SupplierQuotationStatus Status { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn không được phép bỏ trống")]

        public required DateTime QuotationExpiredDate { get; set; }

        public virtual ICollection<QuotationDetailDTO> QuotationDetailDTOs { get; set; }= new List<QuotationDetailDTO>();
    }
}
