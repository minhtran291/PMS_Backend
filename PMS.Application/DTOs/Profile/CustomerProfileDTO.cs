using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.DTOs.Profile
{
    public class CustomerProfileDTO : CommonProfileDTO
    {
        public long? MST {  get; set; }
        public string? ImageCnkd { get; set; }
        public string? ImageByt { get; set; }
        public long? Mshkd { get; set; }


    }
}
