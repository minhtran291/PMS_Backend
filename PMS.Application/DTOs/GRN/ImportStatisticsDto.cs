using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.GRN
{
    public class ImportStatisticsByMonthDto
    {
        public int Year { get; set; }
        public List<MonthlyImportWithProductsDto> MonthlyData { get; set; } = new();
    }
}
