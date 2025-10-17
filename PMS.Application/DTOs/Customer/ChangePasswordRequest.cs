using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Customer
{
    public class ChangePasswordRequest
    {

        public string OldPassword { get; set; } = string.Empty;


        public string NewPassword { get; set; } = string.Empty;
    }
}
