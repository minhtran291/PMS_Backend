using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class CreateWarehouseLocation
    {
        public required int WarehouseId { get; set; }

        [Range(1, 100, ErrorMessage = "Số hàng phải từ 1 đến 100")]
        public required int RowNo { get; set; }

        [Range(1, 100, ErrorMessage = "Số cột phải từ 1 đến 100")]
        public required int ColumnNo { get; set; }

        [Range(1, 10, ErrorMessage = "Số tầng phải từ 1 đến 10")]
        public required int LevelNo { get; set; }
    }
}
