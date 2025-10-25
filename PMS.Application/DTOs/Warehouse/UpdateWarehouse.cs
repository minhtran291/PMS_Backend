using PMS.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Warehouse
{
    public class UpdateWarehouse
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà kho không được để chống")]
        [RegularExpression(@"^[\p{L}0-9\s]+$", ErrorMessage = "Tên nhà kho chỉ được chứa chữ cái và số")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [RegularExpression(@"^[\p{L}0-9\s]+$", ErrorMessage = "Địa chỉ chỉ được chứa chữ cái và số")]
        public required string Address { get; set; }

        public bool Status { get; set; }
    }
}
