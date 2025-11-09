using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class LotCheckDto
    {
        public required int LotID { get; set; }
        public required int ActualQuantity { get; set; }
        public string? Note { get; set; }
    }
}
