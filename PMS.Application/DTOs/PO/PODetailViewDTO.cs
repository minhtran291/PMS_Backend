using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PO
{
    public class PODetailViewDTO
    {
        public  int ProductID {  get; set; }
        public  int Quantity {  get; set; }
        public  decimal UnitPrice {  get; set; }

    }
}
