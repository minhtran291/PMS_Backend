using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class UpdateDraftQuantitiesDTO
    {
        [Required, MinLength(1, ErrorMessage = "Danh sách chi tiết trống.")]
        public List<UpdateDraftQuantityItemDTO> Details { get; set; } = new();
    }
}
