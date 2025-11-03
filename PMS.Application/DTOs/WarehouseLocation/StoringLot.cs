using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.WarehouseLocation
{
    public class StoringLot
    {
        public required int WarehouseId { get; set; }

        [Required(ErrorMessage = "Tên vị trí trong kho không được để trống")]
        public required string LocationName { get; set; }
        public required int LotID { get; set; }
    }
}
