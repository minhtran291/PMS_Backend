using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.Warehouse
{
    public class CreateWarehouse
    {
        [Required(ErrorMessage = "Tên nhà kho không được để chống")]
        [RegularExpression(@"^[\p{L}0-9\s]+$", ErrorMessage = "Tên nhà kho chỉ được chứa chữ cái và số")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [RegularExpression(@"^[\p{L}0-9\s]+$", ErrorMessage = "Địa chỉ chỉ được chứa chữ cái và số")]
        public required string Address { get; set; }
    }
}
