using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.SalesOrder
{
    public class MarkNotCompleteRequest
    {
        public string RejectReason { get; set; } = string.Empty;
    }
}
