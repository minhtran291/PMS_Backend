using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;

namespace PMS.Core.DTO.Content
{
    public class GRNManuallyDTO
    {
        [Required(ErrorMessage = "Source is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public required string Source { get; set; }

        [Required(ErrorMessage = "Total is required.")]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Total { get; set; }
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "Nhà chứa không được để trống")]
        public required int WarehouseLocationID {get; set; }
        public virtual ICollection<GRNDManuallyDTO> GRNDManuallyDTOs { get; set; } = new List<GRNDManuallyDTO>();
    }
}
