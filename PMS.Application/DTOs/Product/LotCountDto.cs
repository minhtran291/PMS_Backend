using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Product
{
    public class LotCountDto
    {
        public int HistoryId { get; set; }          
        public int ActualQuantity { get; set; }      
        public string? Note { get; set; }            
    }
}
