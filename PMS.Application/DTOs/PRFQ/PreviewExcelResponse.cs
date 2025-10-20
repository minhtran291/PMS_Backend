using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PRFQ
{
    public class PreviewExcelResponse
    {
        public string ExcelKey { get; set; } = string.Empty;
        public List<PreviewProductDto> Products { get; set; } = new();
    }
}
