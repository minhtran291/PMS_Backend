using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Enums;

namespace PMS.Application.DTOs.PO
{
    public class PhysicalInventoryUpdateDTO
    {
        public int LotID { get; set; }
        public int ActualQuantity { get; set; }
        [Length(10, 500, ErrorMessage ="độ dài cho phép từ 10 đến 500 ký tự")]
        public string? note {  get; set; }

        public InventorySessionStatus status { get; set; }


    }
}
