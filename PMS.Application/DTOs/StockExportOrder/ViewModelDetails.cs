using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class ViewModelDetails : ListStockExportOrderDTO
    {
        public List<DetailsStockExportOrderDTO> Details { get; set; } = [];
    }
}
