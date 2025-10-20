using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Profile
{
    public class StaffProfileDTO : CommonProfileDTO
    {
        public string EmployeeCode { get; set; } = string.Empty;
    }
}
