using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class UpdateDraftQuantityItemDTO
    {
        public int LotId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity không hợp lệ.")]
        public int Quantity { get; set; }
    }
}
