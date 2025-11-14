using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class FormDataDTO
    {
        public int LotId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime ExpiredDate { get; set; }
        public int Avaiable {  get; set; }

    }
}
