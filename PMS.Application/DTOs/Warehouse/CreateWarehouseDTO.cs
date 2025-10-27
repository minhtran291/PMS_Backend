using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Warehouse
{
    public class CreateWarehouseDTO
    {
        [Required(ErrorMessage = "Tên nhà kho không được để chống")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public required string Address { get; set; }
    }
}
