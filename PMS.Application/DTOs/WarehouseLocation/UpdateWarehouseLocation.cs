using PMS.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class UpdateWarehouseLocation
    {
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Tên vị trí trong kho không được để trống")]
        public required string LocationName { get; set; }
        public bool Status { get; set; }
    }
}
