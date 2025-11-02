using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class ExcelData
    {
        public string SupplierName { get; set; } = string.Empty;
        public int PRFQID { get; set; }
        public int QID { get; set; }
        public DateTime SendDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public bool IsNewQuotation { get; set; }
    }
}
