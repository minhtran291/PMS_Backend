using PMS.Application.DTOs.RequestSalesQuotationDetails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.RequestSalesQuotation
{
    public class UpdateRsqDTO
    {
        public int RsqId { get; set; }
        //public List<UpdateRsqDetailsDTO> Product { get; set; } = [];
        public List<int> ProductIdList { get; set; } = [];
    }
}
