using PMS.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class UpdateWarehouseLocation
    {
        public int WarehouseId { get; set; }

        [Range(1, 100, ErrorMessage = "Số hàng phải từ 1 đến 100")]
        public int RowNo { get; set; }

        [Range(1, 100, ErrorMessage = "Số cột phải từ 1 đến 100")]
        public int ColumnNo { get; set; }

        [Range(1, 10, ErrorMessage = "Số tầng phải từ 1 đến 10")]
        public int LevelNo { get; set; }

        [Range(0, 1, ErrorMessage = "Trạng thái chỉ được nhận giá trị 0 (Inactive) hoặc 1 (Active)")]
        public WarehouseLocationStatus Status { get; set; }
    }
}
