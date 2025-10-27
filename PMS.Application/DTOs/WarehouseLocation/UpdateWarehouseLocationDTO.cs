using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class UpdateWarehouseLocationDTO
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Tên vị trí trong kho không được để trống")]
        public required string LocationName { get; set; }
        public bool Status { get; set; }
    }
}
