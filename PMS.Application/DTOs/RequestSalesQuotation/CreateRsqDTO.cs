using PMS.Application.DTOs.RequestSalesQuotationDetails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.RequestSalesQuotation
{
    public class CreateRsqDTO
    {
        public List<int> ProductIdList { get; set; } = [];
        public int Status {  get; set; }
    }
}
