using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.StockExportOrder
{
    public class FormDataWithDueDateDTO
    {
        public DateTime DueDate {  get; set; }
        public List<FormDataDTO> Details { get; set; } = [];
    }
}
