using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.PaymentRemain
{
    public class CreateBankTransferCheckRequestDTO
    {
        public decimal? Amount { get; set; } 
        public string? CustomerNote { get; set; }  
    }
}
